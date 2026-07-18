terraform {
  required_version = ">= 1.5.0"
  # Remote state in S3 so the state survives between (ephemeral) CI runs — otherwise every run starts
  # blind and collides with the resources the previous run created. Configured at `terraform init`
  # time via -backend-config (bucket/key/region), so nothing account-specific is committed here.
  backend "s3" {}
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.region
}

data "aws_caller_identity" "current" {}

locals {
  bucket_name = var.artifact_bucket_name != "" ? var.artifact_bucket_name : "${var.project}-${data.aws_caller_identity.current.account_id}"

  # The three Cloud Service Lambdas. Each is tagged so discovery finds it; each gets its own HTTP API
  # so its Spec UI's relative fetches resolve cleanly.
  services = {
    orders   = { zip = var.orders_zip,   name = "${var.project}-orders" }
    payments = { zip = var.payments_zip, name = "${var.project}-payments" }
    shipping = { zip = var.shipping_zip, name = "${var.project}-shipping" }
  }
}

# ---------------------------------------------------------------------------------------------------
# S3 bucket for the discovered registry + generated catalog artifacts.
# ---------------------------------------------------------------------------------------------------
resource "aws_s3_bucket" "artifacts" {
  bucket        = local.bucket_name
  force_destroy = true
}

# ---------------------------------------------------------------------------------------------------
# IAM: a basic-execution role for the service Lambdas, and a discovery+invoke+S3 role for the mesh.
# ---------------------------------------------------------------------------------------------------
data "aws_iam_policy_document" "lambda_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "service" {
  name               = "${var.project}-service-role"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume.json
}

resource "aws_iam_role_policy_attachment" "service_logs" {
  role       = aws_iam_role.service.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role" "mesh" {
  name               = "${var.project}-mesh-role"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume.json
}

resource "aws_iam_role_policy_attachment" "mesh_logs" {
  role       = aws_iam_role.mesh.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

data "aws_iam_policy_document" "mesh" {
  # Discover: list all functions and read their tags.
  statement {
    actions   = ["lambda:ListFunctions", "lambda:ListTags"]
    resources = ["*"]
  }
  # Interrogate: invoke the discovered service functions.
  statement {
    actions   = ["lambda:InvokeFunction"]
    resources = [for s in local.services : "arn:aws:lambda:${var.region}:${data.aws_caller_identity.current.account_id}:function:${s.name}"]
  }
  # Persist the registry + catalog artifacts.
  statement {
    actions   = ["s3:GetObject", "s3:PutObject", "s3:ListBucket"]
    resources = [aws_s3_bucket.artifacts.arn, "${aws_s3_bucket.artifacts.arn}/*"]
  }
}

resource "aws_iam_role_policy" "mesh" {
  name   = "${var.project}-mesh-policy"
  role   = aws_iam_role.mesh.id
  policy = data.aws_iam_policy_document.mesh.json
}

# ---------------------------------------------------------------------------------------------------
# The three Cloud Service Lambdas (tagged for discovery) + one HTTP API each.
# ---------------------------------------------------------------------------------------------------
resource "aws_lambda_function" "service" {
  for_each = local.services

  function_name    = each.value.name
  role             = aws_iam_role.service.arn
  filename         = each.value.zip
  source_code_hash = filebase64sha256(each.value.zip)
  runtime          = "provided.al2023"
  handler          = "bootstrap"
  architectures    = [var.lambda_architecture]
  memory_size      = 512
  timeout          = 30

  # The order → payment → shipment chain: each sender gets the next service's queue URL. Only added
  # for services that send (orders, payments); shipping is terminal.
  dynamic "environment" {
    for_each = length(local.service_env[each.key]) > 0 ? [1] : []
    content {
      variables = local.service_env[each.key]
    }
  }

  # Discovery finds services by this tag; the mesh Lambda deliberately does NOT carry it.
  tags = { (var.discovery_tag_key) = "true" }
}

# ---------------------------------------------------------------------------------------------------
# Runtime interconnectivity: SQS ingress queues for the order → payment → shipment chain. orders sends
# payments:capture to the payments queue; payments sends shipping:book to the shipping queue; each
# queue triggers its service Lambda (which already handles SQS via the shared wiring).
# ---------------------------------------------------------------------------------------------------
locals {
  service_env = {
    orders   = { PAYMENTS_QUEUE_URL = aws_sqs_queue.payments.url }
    payments = { SHIPPING_QUEUE_URL = aws_sqs_queue.shipping.url }
    shipping = {}
  }
}

resource "aws_sqs_queue" "payments" {
  name                       = "${var.project}-payments-queue"
  visibility_timeout_seconds = 60
}

resource "aws_sqs_queue" "shipping" {
  name                       = "${var.project}-shipping-queue"
  visibility_timeout_seconds = 60
}

resource "aws_lambda_event_source_mapping" "payments" {
  event_source_arn = aws_sqs_queue.payments.arn
  function_name    = aws_lambda_function.service["payments"].arn
  batch_size       = 1
}

resource "aws_lambda_event_source_mapping" "shipping" {
  event_source_arn = aws_sqs_queue.shipping.arn
  function_name    = aws_lambda_function.service["shipping"].arn
  batch_size       = 1
}

# The shared service role can send to both queues (as a producer) and consume them (the event-source
# mapping polls with the function's role).
data "aws_iam_policy_document" "service_sqs" {
  statement {
    actions = [
      "sqs:SendMessage",
      "sqs:ReceiveMessage",
      "sqs:DeleteMessage",
      "sqs:GetQueueAttributes",
    ]
    resources = [aws_sqs_queue.payments.arn, aws_sqs_queue.shipping.arn]
  }
}

resource "aws_iam_role_policy" "service_sqs" {
  name   = "${var.project}-service-sqs"
  role   = aws_iam_role.service.id
  policy = data.aws_iam_policy_document.service_sqs.json
}

# ---------------------------------------------------------------------------------------------------
# The mesh Lambda (NOT tagged for discovery) + its HTTP API + the aggregation schedule.
# ---------------------------------------------------------------------------------------------------
resource "aws_lambda_function" "mesh" {
  function_name    = "${var.project}-mesh"
  role             = aws_iam_role.mesh.arn
  filename         = var.mesh_zip
  source_code_hash = filebase64sha256(var.mesh_zip)
  runtime          = "provided.al2023"
  handler          = "bootstrap"
  architectures    = [var.lambda_architecture]
  memory_size      = 1024
  timeout          = 60

  environment {
    variables = {
      MESH_ARTIFACT_BUCKET = aws_s3_bucket.artifacts.id
      MESH_ARTIFACT_PREFIX = "mesh"
    }
  }
}

# One HTTP API per Lambda: a $default catch-all proxies the full path through, so each service's
# /benzene/spec-ui and the mesh's /mesh-ui (with their relative fetches) resolve against the API root.
resource "aws_apigatewayv2_api" "service" {
  for_each      = local.services
  name          = "${each.value.name}-api"
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_integration" "service" {
  for_each               = local.services
  api_id                 = aws_apigatewayv2_api.service[each.key].id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.service[each.key].invoke_arn
  payload_format_version = "1.0" # matches what Benzene.Aws.Lambda.ApiGateway parses
}

resource "aws_apigatewayv2_route" "service" {
  for_each  = local.services
  api_id    = aws_apigatewayv2_api.service[each.key].id
  route_key = "$default"
  target    = "integrations/${aws_apigatewayv2_integration.service[each.key].id}"
}

resource "aws_apigatewayv2_stage" "service" {
  for_each    = local.services
  api_id      = aws_apigatewayv2_api.service[each.key].id
  name        = "$default"
  auto_deploy = true
}

resource "aws_lambda_permission" "service_api" {
  for_each      = local.services
  statement_id  = "AllowApiGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.service[each.key].function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.service[each.key].execution_arn}/*/*"
}

resource "aws_apigatewayv2_api" "mesh" {
  name          = "${var.project}-mesh-api"
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_integration" "mesh" {
  api_id                 = aws_apigatewayv2_api.mesh.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.mesh.invoke_arn
  payload_format_version = "1.0"
}

resource "aws_apigatewayv2_route" "mesh" {
  api_id    = aws_apigatewayv2_api.mesh.id
  route_key = "$default"
  target    = "integrations/${aws_apigatewayv2_integration.mesh.id}"
}

resource "aws_apigatewayv2_stage" "mesh" {
  api_id      = aws_apigatewayv2_api.mesh.id
  name        = "$default"
  auto_deploy = true
}

resource "aws_lambda_permission" "mesh_api" {
  statement_id  = "AllowApiGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.mesh.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.mesh.execution_arn}/*/*"
}

# Scheduled aggregation: fire the mesh Lambda with a constant payload the Benzene EventBridge adapter
# routes to the `mesh:aggregate` handler (detail-type = the topic).
resource "aws_cloudwatch_event_rule" "aggregate" {
  name                = "${var.project}-aggregate"
  schedule_expression = var.aggregate_schedule
}

resource "aws_cloudwatch_event_target" "aggregate" {
  rule      = aws_cloudwatch_event_rule.aggregate.name
  target_id = "mesh"
  arn       = aws_lambda_function.mesh.arn
  input     = jsonencode({ "detail-type" = "mesh:aggregate", "source" = "benzene.mesh", "detail" = "{}" })
}

resource "aws_lambda_permission" "mesh_events" {
  statement_id  = "AllowEventBridgeInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.mesh.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.aggregate.arn
}

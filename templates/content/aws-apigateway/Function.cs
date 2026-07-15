using Benzene.Aws.Lambda.Core;

namespace BenzeneStarter;

// AwsLambdaHost<StartUp> builds the pipeline once on cold start and implements the Lambda entry
// point for you - point your function's handler at BenzeneStarter::BenzeneStarter.Function::FunctionHandlerAsync
// (see template.yaml).
public class Function : AwsLambdaHost<StartUp>
{
}

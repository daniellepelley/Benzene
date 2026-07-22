output "mesh_ui_url" {
  description = "Open this in a browser to see the Mesh UI (the catalog of discovered services)."
  value       = "https://${azurerm_linux_function_app.mesh.default_hostname}/mesh-ui"
}

output "mesh_refresh_url" {
  description = "POST here to trigger a discovery + aggregation pass on demand."
  value       = "https://${azurerm_linux_function_app.mesh.default_hostname}/mesh/refresh"
}

output "service_spec_ui_urls" {
  description = "Each service's Spec UI — hosted by the mesh, not the service (the service serves only JSON). The mesh discovers each site by its Function App name, which is the ?service= key here."
  value = { for k, app in azurerm_linux_function_app.service : k =>
  "https://${azurerm_linux_function_app.mesh.default_hostname}/mesh-spec-ui.html?service=${app.name}&manifest=manifest.json" }
}

output "service_function_app_names" {
  description = "The service Function App names to publish the built code to (func azure functionapp publish <name>)."
  value       = [for app in azurerm_linux_function_app.service : app.name]
}

output "mesh_function_app_name" {
  description = "The mesh Function App name to publish the built code to."
  value       = azurerm_linux_function_app.mesh.name
}

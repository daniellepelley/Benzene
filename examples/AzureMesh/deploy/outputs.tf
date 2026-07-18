output "mesh_ui_url" {
  description = "Open this in a browser to see the Mesh UI (the catalog of discovered services)."
  value       = "https://${azurerm_linux_web_app.mesh.default_hostname}/mesh-ui"
}

output "mesh_refresh_url" {
  description = "POST here to trigger a discovery + aggregation pass on demand."
  value       = "https://${azurerm_linux_web_app.mesh.default_hostname}/mesh/refresh"
}

output "service_spec_ui_urls" {
  description = "Each service's Spec UI."
  value       = { for k, app in azurerm_linux_web_app.service : k => "https://${app.default_hostname}/benzene/spec-ui" }
}

output "acr_login_server" {
  description = "The ACR to push the two images to (CI does this before apply)."
  value       = azurerm_container_registry.acr.login_server
}

output "resource_group_name" {
  value = azurerm_resource_group.rg.name
}

output "servicebus_namespace_name" {
  value = azurerm_servicebus_namespace.sb.name
}

output "orders_queue_name" {
  value = azurerm_servicebus_queue.orders.name
}

output "publisher_connection_string" {
  value     = azurerm_servicebus_namespace_authorization_rule.publisher.primary_connection_string
  sensitive = true
}

output "consumer_connection_string" {
  value     = azurerm_servicebus_namespace_authorization_rule.consumer.primary_connection_string
  sensitive = true
}

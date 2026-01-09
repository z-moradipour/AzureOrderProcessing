terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location
}

resource "azurerm_servicebus_namespace" "sb" {
  name                = var.servicebus_namespace_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Basic"
}

resource "azurerm_servicebus_queue" "orders" {
  name               = "orders"
  namespace_id       = azurerm_servicebus_namespace.sb.id
  max_delivery_count = 5
}

resource "azurerm_servicebus_namespace_authorization_rule" "publisher" {
  name         = "publisher-send"
  namespace_id = azurerm_servicebus_namespace.sb.id

  listen = false
  send   = true
  manage = false
}

resource "azurerm_servicebus_namespace_authorization_rule" "consumer" {
  name         = "consumer-listen"
  namespace_id = azurerm_servicebus_namespace.sb.id

  listen = true
  send   = false
  manage = false
}


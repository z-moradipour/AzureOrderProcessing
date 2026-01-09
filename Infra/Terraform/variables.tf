variable "resource_group_name" {
  type        = string
  description = "Resource group name"
  default     = "rg-order-processing-dev"
}

variable "location" {
  type        = string
  description = "Azure region"
  default     = "West Europe"
}

variable "servicebus_namespace_name" {
  type        = string
  description = "Service Bus namespace name (must be globally unique)"
}

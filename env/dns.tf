resource "dnsimple_record" "emotionchecker" {
  domain = "${var.domain}"
  name   = "smile"
  value  = "${azurerm_function_app.emotionfunc.default_hostname}"
  type   = "CNAME"
  ttl    = 3600

  provisioner "local-exec" {
    command = "sleep 30s"
  }
}
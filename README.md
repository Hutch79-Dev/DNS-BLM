# DNS-BLM => DNS Blocklist Monitoring
DNS-BLM is a tool that monitors blocklists (currently utilizing VirusTotal) to check if your domains are flagged as malicious.

## Installation
Copy the Docker Compose file below and update the environment variables.  
Once done, you can start it using `docker compose up -d`.

### Example compose.yaml
```yaml
services:
  dns-blm:
    image: ghcr.io/hutch79/dns-blm:dev
    ports:
      - 7901:8080
    environment:
      - DNS-BLM__API_Credentials__VirusTotal=ENTER-YOUR-VIRUSTOTAL-API-KEY-HERE
      - DNS-BLM__ReportReceiver=reports@example.com
      - DNS-BLM__Mail__Username=resend
      - DNS-BLM__Mail__Password=PASSWORD
      - DNS-BLM__Mail__Host=smtp.resend.com
      - DNS-BLM__Mail__Port=587
      - DNS-BLM__Mail__From=dns-blm@example.com
      - DNS-BLM__Mail__EnableSsl=true
      - DNS-BLM__Domains__0=example.ch
      - DNS-BLM__Domains__1=example.org
      - DNS-BLM__Domains__2=example.com
```

## Environment Variables

To run this project, you will need to add the following environment variables to your .env file


| **Variable**                             | **Description**                                      | **Required**                     |
|------------------------------------------|------------------------------------------------------|----------------------------------|
| `DNS-BLM__API_Credentials__VirusTotal`   | VirusTotal API Key                                   | Yes                              |
| `DNS-BLM__ReportReceiver`                | Email address to receive scanning reports            | Yes                              |
| `DNS-BLM__Mail__Username`                | Email username for SMTP                              | Yes                              |
| `DNS-BLM__Mail__Password`                | Email password for SMTP                              | Yes                              |
| `DNS-BLM__Mail__Host`                    | SMTP server host                                     | Yes                              |
| `DNS-BLM__Mail__Port`                    | SMTP server port                                     | Yes                              |
| `DNS-BLM__Mail__From`                    | Sender email address                                 | Yes                              |
| `DNS-BLM__Mail__EnableSsl`               | Enable SSL for SMTP?                                 | Yes                              |
| `DNS-BLM__Domains__X`                    | Replace `X` with a number: Domain to scan            | Yes (minimum 1 domain required)  |

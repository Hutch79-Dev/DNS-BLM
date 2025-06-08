# DNS-BLM => DNS Blocklist Monitoring
DNS-BLM is a tool that monitors blocklists (currently utilizing VirusTotal) to check if your domains are flagged as malicious.

## Installation
Copy the Docker Compose file below and update the environment variables.  
Once done, you can start it using `docker compose up -d`.

### Example compose.yaml
```yaml
services:
  dns-blm:
    image: ghcr.io/hutch79/dns-blm:0
    environment:
      - TZ=Europe/Zurich
      - DNS-BLM__API_Credentials__VirusTotal=YourSuperSecretApiKey
      - DNS-BLM__ReportReceiver=YourReportRecivingMailAddress@example.com
      - DNS-BLM__Mail__Username=FunnyName
      - DNS-BLM__Mail__Password=SuperSecurePassword
      - DNS-BLM__Mail__Host=smtp.BestestMailServer.ch
      - DNS-BLM__Mail__Port=587
      - DNS-BLM__Mail__From=WoopsyDopsySomethingIsNotGood@BestestMail.ch
      - DNS-BLM__Mail__EnableSsl=true
      - DNS-BLM__Domains__0=BestestMail.ch
      - DNS-BLM__Domains__1=BestestMailServer.ch
      - DNS-BLM__TimedTasks__ScanBlacklistProviders=0 2 * * * # Executed daily at 2am
```

## Environment Variables

To run this project, you will need to add the following environment variables to your .env file


| **Variable**                                   | **Description**                                      | **Required**                     |
|------------------------------------------------|------------------------------------------------------|----------------------------------|
| `TZ`                                           | Your local Timezone                                  | No (Defaults to UTC)             |
| `DNS-BLM__API_Credentials__VirusTotal`         | VirusTotal API Key                                   | Yes                              |
| `DNS-BLM__ReportReceiver`                      | Email address to receive scanning reports            | Yes                              |
| `DNS-BLM__Mail__Username`                      | Email username for SMTP                              | Yes                              |
| `DNS-BLM__Mail__Password`                      | Email password for SMTP                              | Yes                              |
| `DNS-BLM__Mail__Host`                          | SMTP server host                                     | Yes                              |
| `DNS-BLM__Mail__Port`                          | SMTP server port                                     | Yes                              |
| `DNS-BLM__Mail__From`                          | Sender email address                                 | Yes                              |
| `DNS-BLM__Mail__EnableSsl`                     | Enable SSL for SMTP?                                 | Yes                              |
| `DNS-BLM__Domains__X`                          | Replace `X` with a number: Domain to scan            | Yes (minimum 1 domain required)  |
| `DNS-BLM__TimedTasks__ScanBlacklistProviders`  | Cron expression when to execute the scan             | Yes                              |

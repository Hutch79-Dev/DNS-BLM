# DNS-BLM => DNS Blocklist Monitoring
**TL;DR**: DNS-BLM is a tool that monitors blocklists (currently utilizing VirusTotal) to check if your domains are flagged as malicious, and notifys you if they are.

Maybe you know the trouble of Block lists.
You just want to quickly go to one of your services at school or at work, and it does not load. Maybe you try it with your mobile and then notice your domain is blocked. After digging around a bit, you may figure out it's listed on a Block list.
It happened to me twice now, and I hate it. Especially since I noticed it after it's already too late.

That's why I created DNS-BLM (DNS Block List Monitor). It uses VirusTotal to scan your domains against ~95 Block lists and notify you by mail if they're listed as suspicious or malicious.

## Future development

To plan future development, I use issues and categorize them into Milestones. Every Milestone represents a List of tasks which need to be done before the next minor or major release. 

Bug fixes do not necessarily get tracked by Milestones.

A list of current Milestones can be found here: [Milestones](https://github.com/Hutch79/DNS-BLM/milestones?sort=title&direction=asc)

## Installation
Copy the Docker Compose file below and update the environment variables.  
Once done, you can start it using `docker compose up -d`.

### Example compose.yaml
```yaml
services:
  dns-blm:
    image: ghcr.io/hutch79/dns-blm:1
    environment:
      - TZ=Europe/Zurich
      - DNS-BLM__ApiCredentials__VirusTotal=YourSuperSecretApiKey
      - DNS-BLM__ReportReceiver=YourReportRecivingMailAddress@example.com
      - DNS-BLM__Mail__Username=FunnyName
      - DNS-BLM__Mail__Password=SuperSecurePassword
      - DNS-BLM__Mail__Host=smtp.BestestMailServer.ch
      - DNS-BLM__Mail__Port=587
      - DNS-BLM__Mail__From=WoopsyDopsySomethingIsNotGood@BestestMail.ch
      - DNS-BLM__Mail__EnableSsl=true
      - DNS-BLM__Domains__0=BestestMail.ch
      - DNS-BLM__Domains__1=BestestMailServer.ch
      - DNS-BLM__TimedTasks__ScanBlacklistProviders=9 6 * * * # Executed daily
```

## Environment Variables

To run this project, you will need to add the following environment variables to your .env file



| **Variable** | **Description** | Default | **Required** |
|----|----|----|----|
| `TZ` | Your local Timezone | UTC | No |
| `DNS-BLM__API_Credentials__VirusTotal` | VirusTotal API Key |    | Yes |
| `DNS-BLM__ReportReceiver` | Email address to receive scanning reports |    | Yes |
| `DNS-BLM__Mail__Username` | Email username for SMTP |    | Yes |
| `DNS-BLM__Mail__Password` | Email password for SMTP |    | Yes |
| `DNS-BLM__Mail__Host` | SMTP server host |    | Yes |
| `DNS-BLM__Mail__Port` | SMTP server port | 587 | No |
| `DNS-BLM__Mail__From` | Sender email address |    | Yes |
| `DNS-BLM__Mail__EnableSsl` | Enable SSL for SMTP? | True | No |
| `DNS-BLM__Domains__X` | Replace `X` with a number: Domain to scan |    | Yes (minimum 1 domain required) |
| `DNS-BLM__TimedTasks__ScanBlacklistProviders` | Cron expression when to execute the scan | 20 4 \* \* \* (4:20 am) | No |

# Home Automation
Repo for my private solution for running smart things in my home, and of course its a work in progress (aka never to completed) as private projects should be.

But why a public GitHub? Why not share the code if someone else is interested, its always fun if it can help someone.

## Why build it yourself?

Well, why not? Its fun to build stuff and tinker around with smart devices. Currently its more of a side-project in the house and nothing to rely on, but hopefully it can grow over the years.

Also, this way I can keep all of the device data and logic offline and have full control over that while I also can try to connect the cheaper devices I get a hold on instead of just buying one brand or one type of device.

## Quality and security

Pull requests run .NET analyzer and style checks at information severity, a warning-free Release build, the full test suite, dependency auditing, and maintained-code coverage enforcement.

Run the same checks locally with:

```powershell
dotnet restore HomeAutomation.sln -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet format HomeAutomation.sln analyzers --verify-no-changes --severity info --no-restore --exclude HomeAutomation.Database/Migrations
dotnet format HomeAutomation.sln style --verify-no-changes --severity info --no-restore --exclude HomeAutomation.Database/Migrations
dotnet build HomeAutomation.sln -c Release --no-restore -t:Rebuild
dotnet test HomeAutomation.sln -c Release --no-build --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory TestResults
./Tools/Assert-Coverage.ps1 -ResultsDirectory TestResults -MinimumLineCoverage 29
```

See [SECURITY_REVIEW.md](SECURITY_REVIEW.md) for the current threat model, prioritized findings, and implementation guidance.

## Roadmap

Currently these are things that are in plan:

* Integrate my Ikea hub
* Integrate my home alarm (verisure)
* enable the system to sit on the wall with a tabelet
* support to set up filters on triggers
* fetch and show weather data
* show sensors in a good way
* move database.json to the postgres database
* have a memory state for the devices to setup more advanced flows

## Integrations

Currently integrates the following:

* [ZWaveLib WebAPI](https://github.com/trembon/ZWaveLib.WebAPI) for sending and receiving events for 868 MHz devices
* [TelldusCoreWrapper WebAPI](https://github.com/trembon/TelldusCoreWrapper.WebAPI) for sending and receiving events for 433 MHz devices.
* [tuya-local-api](https://github.com/trembon/tuya-local-api) for sending and receiving events for Tuya devices, offline and locally.
* [SmtpServer](http://cainosullivan.com/smtpserver) for translating mails to events from not-so-smart devices, like my IP cameras.
* [sunrise-sunset](https://sunrise-sunset.org/) for fetching sunrise and sunset data.
* Renhållningen Kristianstad for waste collection schedules and reminders.
* [Slack](https://slack.com/) for free "sms" to my phone with whats happening.
* [Synology DSM](https://www.synology.com/dsm) for forwarding NAS warnings and critical notifications to Slack.

## Waste collection configuration

Keep the address-specific API URL and unit ID outside source control. They can be set in the ignored `HomeAutomation/appsettings.Development.json` file under `WasteCollection:ApiUrl` and `WasteCollection:UnitId`, or through the `APP_WasteCollection__ApiUrl` and `APP_WasteCollection__UnitId` environment variables.

The checked-in defaults fetch once per day after 02:00 and send a Slack reminder to `events` after 10:00 on the day before collection. These can be changed with `WasteCollection:FetchHour`, `WasteCollection:NotificationHour`, and `WasteCollection:SlackChannel`.

## Synology DSM notification configuration

HomeAutomation can receive warning and critical notifications from a Synology NAS and forward them to Slack. The webhook only accepts direct requests from IP addresses listed in `Synology:AllowedIpAddresses`; forwarded IP headers are deliberately ignored.

Give the NAS a fixed IP address or DHCP reservation, then configure the address outside source control. For a container deployment, the environment settings can look like:

```text
APP_Synology__AllowedIpAddresses__0=192.168.1.20
APP_Synology__SlackChannel=errors
```

Restart HomeAutomation after changing its configuration. The NAS must call the app directly over the LAN, using a URL such as `http://<home-automation-host>:<published-port>/webhooks/synology/notification`. Do not put a reverse proxy between DSM and this endpoint because the proxy address would replace the NAS source address.

In DSM 7.3.2:

1. Open **Control Panel → Notification → Webhooks** and click **Add**.
2. Select **Custom** as the provider and the built-in **Warning** rule. This rule includes warning and critical events.
3. Set the provider name to `HomeAutomation Slack errors`, set the subject to the NAS hostname, and enter the HomeAutomation webhook URL.
4. Click **Next**, select `POST`, and select `application/json`.
5. Set the HTTP body to `{"text":"@@TEXT@@"}`. Do not add an authentication header.
6. Apply the configuration and click **Send Test Message**.
7. Verify the notification appears in Slack channel `errors`. The configured Slack bot must have `chat:write` permission and be a member of the channel.

See Synology's documentation for [notification rules](https://kb.synology.com/en-global/DSM/help/DSM/AdminCenter/system_notification_rule?version=7) and [custom webhook configuration](https://kb.synology.com/en-global/DSM/help/DSM/AdminCenter/system_notification_webhook?version=7).

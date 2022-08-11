# Home Automation
Repo for my private solution for running smart things in my home, and of course its a work in progress (aka never to completed) as private projects should be.

But why a public GitHub? Why not share the code if someone else is interested, its always fun if it can help someone.

## Why build it yourself?

Well, why not? Its fun to build stuff and tinker around with smart devices. Currently its more of a side-project in the house and nothing to rely on, but hopefully it can grow over the years.

Also, this way I can keep all of the device data and logic offline and have full control over that while I also can try to connect the cheaper devices I get a hold on instead of just buying one brand or one type of device.

## Roadmap

I have some Tuya devices that I want to bring offline and try to integrate with this solution, but if a solution for that comes up its probably will be under the integrations part.

There is also an older Android tablet that was bought with the purpose to sit on the wall and control everything.

Otherwise, try to make the solution more stable without bugs (like that will happen).

## Integrations

Currently integrates the following:

* [ZWaveLib WebAPI](https://github.com/trembon/ZWaveLib.WebAPI) for sending and receiving events for 868 MHz devices
* [TelldusCoreWrapper WebAPI](https://github.com/trembon/TelldusCoreWrapper.WebAPI) for sending and receiving events for 433 MHz devices.
* [tuya-local-api](https://github.com/trembon/tuya-local-api) for sending and receiving events for Tuya devices, offline and locally.
* [SmtpServer](http://cainosullivan.com/smtpserver) for translating mails to events from not-so-smart devices, like my IP cameras.
* [YR.no](https://api.met.no/) for fetching sunrise and sunset data (and also storing weather data for future use).
* [Slack](https://slack.com/) for free "sms" to my phone with whats happening.


## Notes

Yes, the web UI is crap, but everything its mostly notifications in Slack on my phone based on the JSON configuration.

But, you have SignalR set up? Yes, though its currently only used for debugging and showing live messages from connected devices.
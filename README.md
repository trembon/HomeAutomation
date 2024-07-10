# Home Automation
Repo for my private solution for running smart things in my home, and of course its a work in progress (aka never to completed) as private projects should be.

But why a public GitHub? Why not share the code if someone else is interested, its always fun if it can help someone.

## Why build it yourself?

Well, why not? Its fun to build stuff and tinker around with smart devices. Currently its more of a side-project in the house and nothing to rely on, but hopefully it can grow over the years.

Also, this way I can keep all of the device data and logic offline and have full control over that while I also can try to connect the cheaper devices I get a hold on instead of just buying one brand or one type of device.

## Roadmap

Currently these are things that are in plan:

* Integrate my Ikea hub
* Integrate my home alarm (verisure)
* enable the system to sit on the wall with a tabelet
* support to set up filters on triggers
* show sensors in a good way
* fetch information about when trash is due for pickup
* move database.json to the postgres database
* have a memory state for the devices to setup more advanced flows

## Integrations

Currently integrates the following:

* [ZWaveLib WebAPI](https://github.com/trembon/ZWaveLib.WebAPI) for sending and receiving events for 868 MHz devices
* [TelldusCoreWrapper WebAPI](https://github.com/trembon/TelldusCoreWrapper.WebAPI) for sending and receiving events for 433 MHz devices.
* [tuya-local-api](https://github.com/trembon/tuya-local-api) for sending and receiving events for Tuya devices, offline and locally.
* [SmtpServer](http://cainosullivan.com/smtpserver) for translating mails to events from not-so-smart devices, like my IP cameras.
* [YR.no](https://api.met.no/) for fetching sunrise and sunset data (and also storing weather data for future use).
* [Slack](https://slack.com/) for free "sms" to my phone with whats happening.

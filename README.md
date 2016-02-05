# Snowflake-Lite .NET
### Overview

This is a lightweight implementation of [Twitter's Snowflake algorithm](https://github.com/twitter/snowflake/) on .NET platform. The library consists of only one class `IdFactory`, which is pretty much equal to [IdWorker](https://github.com/twitter/snowflake/blob/snowflake-2010/src/main/scala/com/twitter/service/snowflake/IdWorker.scala). However, there are still few main differences:

* When generating new ID, the `IdFactory` class employs spin lock instead of mutex to have faster performance.
* The default constructor of `IdFactory` class will extract local IP addresses as its worker ID and data center ID.
* Codes and exception messages are adjusted to follow [C# coding conventions](https://msdn.microsoft.com/en-us/library/ff926074.aspx).

# Esp32Blasphemy

> Beware: This code is ugly.

Welcome to my den of ~~tortures~~ experiments with poor ESP32 board and nanoFramework. This was a quick ~~-ish~~ intellectual trip to see how .Net performs in the embedded world and I'd like to note my experiences here if any of you brave travellers would be in need of such.

## Scope

There are two parts of this "system":
* ESP32 Board running nanoFramework
* Some other random device running .Net 6

Board is:
* Reading Rfid Card Type A via PN532 connected via HSU (High Speed Uart)
* Publishing this data via:
    * Mqtt publish
    * Http endpoint
* Exposing diagnostic data:
    * Rfid miss/scan counters
    * Memory usage
    * CPU usage
        * Which originates from custom C++ Interop code

Some other random device is:
* Spinning up mqtt server to which board is publishing messages
* Sending Http requests for the same Rfid data as it arrives from Mqtt
* Saving incoming communications to files with timestamps

## Details

### Common

As it appears you can share code between .Net6 and nanoFramework assembly relatively easily by facilitating the means of so called "Shared" projects, funnily enough I've never known about it before so some knowledge was gained here though I'm not sure if there's any other use for it when runtime supports the "normal" targets like .net6 with .netstandard2.0/2.1.

### Torture - the nanoFramework project

#### Inception

The meat of this project, and it literally was in my very first iteration of it. Making Http requests to [BaconIpsum.com](https://baconipsum.com) for the the tasty and meaty LoremIpsum variation which now lays partially commented on the bottom of `Program.cs` file. Quick test of Json deserialization and hooking up cert of TLS from resource files which are quite cool. As it later turned out the certificates can be uploaded from the DeviceExplorer tab in the Visual Studio via the nanoFramework Extension (as well as WiFi configuration). Which can be still done easily from code.

```cs
var wifi =
    WifiNetworkHelper.ScanAndConnectDhcp(
        "SSID",
        "Password",
        WifiReconnectionKind.Automatic,
        true);
```

All reconnects and such worked as expected.

#### Rfid

My second big goal was to connect some of this shiny hardware hanging on wobbly breadboard cables... Luckily there's a ton of common device/sensor libraries available so integration was just a matter of copying the sample, running it aaaand discovering it was not possible to establish connection with my lovely PN532. Thus I got to checking the cables, first thing was discovering that when the ESP32 has enabled Wi-Fi connectivity the number of pins are disabled for some reason, which included one HSU pin which I meticulously checked the ESP devkit board pinout for. I thought I was done for but after digging through samples it appears the pinout is mostly irrelevant as the embedded system gods added option to configure which pin does what... #magic. 

```cs
Configuration.SetPinFunction(22, DeviceFunction.COM2_TX);
Configuration.SetPinFunction(21, DeviceFunction.COM2_RX);
```

Now... me being _very_ newbish to the embedded stuff I lost an hour of my life debugging why my PN532 still refused to connect... and... well... yeah... let's just say you are supposed to cross the TX and RX between the device and board... Fun.

After that brain-fart everything worked as expected.

One sad thing about the library is that it is bugged to a degree,

```cs
_device = new Pn532("COM2");
```

You create the device and provide the name of the COM port to connect, sweet. The not so sweet part (as of time of writing) is that currently that library does not dispose the serial stream it creates on failure (_ref. wobbly breadboard cables_) aaand it throwing an exception from constructor prohibits us from even accessing that stream to close it manually, reflection won't work either since we don't even have the reference as the constructor threw. Not a deal breaker and fairly fixable with a simple pull request I guess.

```cs
if (!this.IsPn532())
    throw new Exception("Can't find a PN532");
```

_RIP in peace._

#### Cpu statistics

And now it's time for the cherry on top. Interop stuff, C++ stuff, `external` keyword and all that swag.

Having obtained memory statistics I _yearned_ to get the %CPU/MCU of the board to see how "heavy" the nanoFramework is while it's doing its thing. And while memory is a matter of single line of C#:
```cs
NativeMemory.GetMemoryInfo(
    NativeMemory.MemoryType.Internal,
    out var totalSize,
    out var freeSize,
    out var largestFreeBlock);
```
the CPU stats... are not. In fact you have to build your very own custom `nanoCLR.bin` from the sources with some customizations. Given it's more of a debug data it makes sense for it not being included in the standard public image. I'm sometimes ambitious and sometimes curious, when stars align and I'm in both states I get to work. Let's say it was interesting trip not having any prior experience with embedded programming nor any legitimate C++ codebases.

##### First steps

Initially I stumbled upon stubs, which Visual Studio generates for you when you add something like:
```cs
[MethodImpl(MethodImplOptions.InternalCall)]
private static extern ushort GetCpuUsageInternal(sbyte[] buffer);
```
in your code.

Initially I thought "Great now I'm just gonna fill it with C++, everything will automatically build, job done". Except it didn't obviously. As it appears the generated bunch of files is a CMake module which you throw inside the sources of your local cloned [repo](https://github.com/nanoframework/nf-interpreter). Having my keyword _stub_ with a quick search engine query I land at this marvel: https://jsimoesblog.wordpress.com/2018/06/19/interop-in-net-nanoframework/ , fairly dated but still relevant.

##### Building the image

After scrolling through the article I ventured to the nanoFramework docs to find any tips for building the CLR myself from sources and to my surprise the process is very smooth and well designed: https://docs.nanoframework.net/content/building/using-dev-container.html Bam!
Pull a docker container with everything inside, run it and you are golden. Launch VS Code, select configuration, let build, **it builds** and there's something to work with. Not having a speck of experience with CMake I was a tad confused with preset files. When they say:
> Step 6: copy the file in CMakeUserPresets-TEMPLATE.jsonto CMakeUserPresets.json

Just don't replace it in its entirety like me... and just merge the json arrays with presets. The ones inside inherit the pasted ones and without there being base presets nothing appears to work and CMake error messages are not that helpful. From now on it was smooth sailing tooling wise.

Now you copy the folder from `Stubs` that Visual Studio generated into handy `InteropAssemblies` forlder in the root of the [nf-interpreter repo](https://github.com/nanoframework/nf-interpreter). Except for the `*.cmake` file, this bugger goes to `CMake/Modules` and after that the build system will do its magic _automagically_ _sparkle sparkle_.

So now, I have project ready to get the implementation going, essentially, it's best to just go to the part where it reads:
```cpp
    ////////////////////////////////
    // implementation starts here //

```

and 

```cpp

    // implementation ends here   //
    ////////////////////////////////
```

Very handy. One other issue I had which I did not explore was broken header files with member signatures like these:

```cpp
    static const int FIELD_STATIC__<CurrentMessage>k__BackingField = 0;
    static const int FIELD_STATIC__<Error>k__BackingField = 1;
    static const int FIELD_STATIC__<DataCounter>k__BackingField = 2;
    static const int FIELD_STATIC__<MqttConnection>k__BackingField = 3;
    static const int FIELD_STATIC__<NfcMissCounter>k__BackingField = 4;
```

Oh no!... Anyway...

Commenting these out did the trick and as I did not need them it was fine.

#### Implementing the logic

CPU stats are quite finnicky and depend on variety of factors and circumstances and are domain of the RTOS running on the board, in my case the RTOS for the ESP32 was FreeRTOS, let's jump into docs... or rather reddit was the first helpful find where supposedly creator of windows' early task manager was on similar quest as mine: https://www.reddit.com/r/esp32/comments/eplwhb/handydandy_cpu_idle_time_calc_class_for_you/ and comments neatly forward you to this gem: https://github.com/espressif/esp-idf/tree/master/examples/system/freertos/real_time_stats Yay! This is where I adapted the code from, it's clean and it works. A bit of refactor to work on demand from HTTP request and we are done. 

I figured out I'll let it generate the string for me by populating a buffer for which memory is allocated on the C# side of things.

```cs
private readonly sbyte[] _statsBuffer = new sbyte[1024];
```

and it arrives into my C++ method like this:

```cpp
uint16_t CpuStatsProvider::GetCpuUsageInternal( CLR_RT_TypedArray_INT8 param0, HRESULT &hr )
```

returned type will be the number of bytes (characters in this case) written to the buffer. The `CLR_RT_TypedArray_INT8` type translates basically to `char*` to which I write the lines instead of using `printf` like original sample. 

```cpp
outputBufferIndex += snprintf(outputBuffer + outputBufferIndex, RETURN_BUFFER_SIZE - outputBufferIndex, "| Task | Run Time | Percentage\n");
```

One last obstacle was figuring out why call to:

```cpp
uxTaskGetSystemState(start_array, start_array_size, &start_run_time);
```

was leaving the `start_run_time` at `0` which did not allow me to obtain my precious data. Thankfully the [docs](https://www.freertos.org/uxTaskGetSystemState.html) had a tip that I need to compile the image with config value of `configGENERATE_RUN_TIME_STATS` set to `1`. I did as instructed and it worked just fine. Just the note that the required header file `FreeRTOSConfig.h` is not in the cloned repo but resides in `sources/esp-idf/components/freertos/include/freertos/FreeRTOS.h` (in the docker container at least). The proper way would be to work with one of the config files I guess but it didn't interest me. After building the image one more time this beautiful text appeared:

```
| Task | Run Time | Percentage
| main_task | 956837 | 0%
| spin1 | 47159249 | 0%
| IDLE | 126423222 | 8%
| IDLE | 72483749 | 6%
| spin5 | 45748120 | 0%
| Tmr Svc | 5423 | 0%
| tiT | 244455 | 0%
| spin3 | 44071433 | 0%
| spin2 | 44064546 | 0%
| spin0 | 44059709 | 0%
| StorageIOTask | 68601 | 0%
| ReceiverThread | 22245 | 0%
| spin4 | 49574540 | 1%
| sys_evt | 0 | 0%
| uart1_events | 0 | 0%
| esp_timer | 334485 | 0%
| wifi | 3086917 | 0%
| ipc0 | 0 | 0%
| ipc1 | 0 | 0%
```

I'm aware there's something like this available too for stats but is less fun and configuring measurement time is not as explicit: https://www.freertos.org/a00021.html#vTaskGetRunTimeStats

## Closing thoughts

This concludes my journey for now, I hope that someone may stumble upon this and find some help possibly as there's not that much info on the topic floating around. Be aware that I'm fairly random person without any prior experience and code on this repo is plain ugly as it's just my sandbox of sorts.

Cpp files can be found in: `InteropSources` folder.
Rest of the solution is usual `.sln` file.
Built binary with CMake presets is in `Binaries` folder. The image was build from [d7cda02](https://github.com/nanoframework/nf-interpreter/commit/a7af0ae4e34048a26c360a05ec00a370f5d187de) commit on nf-interpreter repository.

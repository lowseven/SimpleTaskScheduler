# SimpleTaskScheduler

![Nuget](https://img.shields.io/nuget/v/TPLSimpleTaskScheduler)
![codecov](https://img.shields.io/codecov/c/gh/lowseven/SimpleTaskScheduler?style=flat) ![license: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=flat)

[__Buy me a coffee! :coffee:__](https://www.buymeacoffee.com/lowseven)


## Intro

The main purpose behind this simple solution is to have some of the new task management features on older 
.NET applications and, of course, testing some of the TPL classes that the library provides.

## Dependencies

- [Serilog]() for logging.
- [Moq](https://github.com/moq/moq) and [xUnit](https://github.com/xunit/xunit) for the test suite
- [FluentAssertion](https://github.com/fluentassertions/fluentassertions) for the assertions

## Get started


```cs
var scheduler = new TPLTaskScheduler();
var workItem = new WorkItem(() => 
{
  Thread.Sleep(2 * 1000);
  Console.WriteLine("something is happening here uh"); 
});

scheduler.EnqueueWork(workItem); //eventually it will be completed

//or if you want use the await/async keyworkds 

var bananas = new WorkItem<string>(() => "🍌🍌🍌");
var monkeys = new WorkItem<string>(() => "🙊🙊🙊");

var mon = await monkeys; //if you use the IWorkItem<Type> interface

await bananas;//if you use the plain IWorkItem interface

Console.WriteLine(mon);
Console.WriteLine(bananas.Result);

```

## More usages

```cs

var task = Task.Factory.StartNew(
  () => 
  { 
    Thread.Sleep(2 * 1000);
    Console.WriteLine("🙊 eats the 🍌 after a while");
  }
  , CancellationToken.None
  , TaskCreationOptions.None
  , scheduler);

```
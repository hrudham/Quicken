Quicken
=======

A task launcher similar to Launchy. It has been written specifically for Windows 8.1.

![alt tag](Example.gif)

## How do I use it?

1. Press **ALT + SPACE** to show the application from anywhere on our desktop.
2. Type to search for your application or link by name.
3. Press the **UP** or **DOWN** keys to iterate through targets that match your query.
4. Press **ENTER** to run your selected target, or press **SHIFT + ENTER** to run it as an administrator (the latter for desktop applications only).

Note that Quicken learns from what you type in. For example, if you type *word* and it selects **Wordpad**, but then you change the selection to **Microsoft Word** and execute it, the next time you type in *word*, it will highlight **Microsoft Word** instead.

##Why did you build it?

*Launchy already exists? Why don't you just use that?*

I really like how Launchy works. However, there are a few issues I have with it:

It cannot do a couple Windows specific things, albeit for good reason, since it's OS agnostic. However, I want to be able to do things such as running an application as an administrator, run ".appref-ms" links, or launch Metro Applications.

Launchy also hasn't been updated in quite a while as well, and the source code is not available, so unfortunately I can't just fix the issues there.

*The Windows 8.1 start-menu does most of this already. Why re-invent the wheel?*

I find that the start menu user experience can be quite jarring; it takes over your screen completely as it leaves your desktop, which I feel isn't a great experience, especially if you actually want to continue using your desktop.

In addition, in order to launch an application as an administrator, you are forced to use your mouse, or a seriously convoluted combination of keys in order to get there.

*Any other reasons why you wrote this?*

I've never used WPF before; what better way is there to learn it than building a project with it? This is also just a personal project at heart. I quite simply just wanted to do it.

##FAQ

*I can't run x64 applications! What's wrong?*

You probably have the x86 version of Quicken installed; install the x64 version instead to fix this.

*Why won't Metro applicatons launch when I run build and run this through Visual Studio?*

You're probably running Visual Studio with administrator privileges turned on. Try turning it off.

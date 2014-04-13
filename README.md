SimpleLogParser
===============

A simple, pluggable log parsing application/library written in C#. Used to read/alert on log files.

Sadly there are still some threading issues with my parallel for loops that need to be worked out, so if you use this code you will want to single thread it or fix the threading issues.
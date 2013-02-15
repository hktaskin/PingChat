PingChat
========

PingChat software is written in C# using Visual Studio 2012.

It allows to chat on LAN using the ICMP. Since, ICMP is mostly not blocked (and also not logged) on firewalls, it easily bypasses the firewall and enables chatting. Data is sent unencrypted through ICMP Payload. It primarily works on LAN because there is no NAT (problem?) on LAN. But in appropriate circumstances and under proper settings, it is possible to have it work on WAN.

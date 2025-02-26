#!/bin/bash
cd /home/shibaguin/Documents/Учёба/Диплом/Atune/Atune
rm -rf /home/shibaguin/Documents/Учёба/Диплом/Atune/Atune/Data/Migrations
dotnet ef migrations add InitialCreate -o Data/Migrations
rm /home/shibaguin/Documents/Учёба/Диплом/Atune/Atune/media_library.db
dotnet ef database update
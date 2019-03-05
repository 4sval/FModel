@echo off
title Embed DLLs


libz inject-dll --assembly "FModel.exe" --include *.dll --move
del /S *.xml
del /S *.pdb
del /S *.config
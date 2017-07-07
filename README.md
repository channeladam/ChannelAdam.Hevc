# ChannelAdam.Hevc

## Overview
An HEVC / H.265 C# library.

Provides an INCOMPLETE implementation for parsing and manipulating the data in an HEVC / H.265 byte stream, WITHOUT re-encoding and changing the video quality.

*The .NET Core console application can currently change the Sample Aspect Ratio (SAR) that is encoded into the video stream.*


## Usage
To change the Sample Aspect Ratio of a HEVC video:

1. You need to de-mux or extract the raw HEVC / H.265 video stream - using a tool such as FFMpeg or MkvExtract.
    * e.g. ffmpeg -i "myvideo.h265.mkv" -vcodec copy -an "myvideo.hevc"
    * e.g. mkvextract tracks "myvideo.h265.mkv" 0:"myvideo.hevc"

2. Run this console application to change the SAR enbedded within that video stream.
    e.g. C:\ChannelAdam.Hevc\ChannelAdam.Hevc.NalUnitChanger.Console> dotnet run /in="myvideo.hevc" /out="out.hevc" /sarWidth=1 /sarHeight=1
    
3. Use your favourite tool (e.g. FFMPeg, MkvMerge, Mkvtoolnix-gui, etc) to re-mux the video stream with your original audio stream to bring it all back together again.

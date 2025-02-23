using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ioSenderTouch.Utility;

public class GcodeSurfacingBuilder
{

    private const string End = "M30";
    private const string TurnOffFlood = "M9";
    private const string Flood = "M8";
    private const string Mist = "M7";
    private const string TravelFeedRate = "G1";
    private const string RapidFeedRate = "G0";
    private const string Metric = "G21";
    private const string AbsolutePos = "G91";
    private const string IncrementalPos = "G90";
    private const string X0 = "X0";
    private const string Y0 = "Y0";
    private const string SpindleOn = "M3";
    private const string SpindleOff = "M5";
    private const string Pause = "G4";

    private double _feedRate;
    private double _toolDiameter;
    private double _width;
    private double _length;
    private int _numberOfPasses;
    private double _depth;
    private double _rpm;
    private double _safeHeight;
    private readonly bool _mist;
    private readonly bool _flood;
    private double _overLapPercent;
    private  CultureInfo _oldCulture;

    public string FilePath { get; set; }
    public GcodeSurfacingBuilder(double width, double length, double feedRate, double dia, int numberOfPasses, double depth, double overlap, double rpm, double safeHeight, bool mist , bool flood)
    {
        using (new TemporaryThreadCulture( new CultureInfo("en-US")))
        {
            _width = width;
            _length = length;
            _feedRate = feedRate;
            _toolDiameter = dia;
            _numberOfPasses = numberOfPasses;
            _depth = depth;
            _rpm = rpm;
            _safeHeight = safeHeight;
            _mist = mist;
            _flood = flood;
            _overLapPercent = overlap / 100;
            GenerateGcode();
        }
            
    }
    private void GenerateGcode()
    {
        var gcodeList = new List<string>();
        gcodeList.AddRange(BuildHeader());
        //TODO remove ramp for now
        //gcodeList.AddRange(BuildGcodeRamp());
        gcodeList.AddRange(BuildSurfaceLayer());
        gcodeList.AddRange(BuildShutDown());
        WriteFile(gcodeList);
    }
    private List<string> BuildHeader()
    {
        var header = new List<string>
        {
            ";Header",
            Metric,
            IncrementalPos,
            $"{RapidFeedRate}{X0}{Y0}Z{_safeHeight:f4}",
            $"{TravelFeedRate}F{_feedRate}",
            $"{SpindleOn}S{_rpm}",
            $"{Pause}P4",
        };

        if (_mist)
        {
            header.Add(Mist);
        }
        if (_flood)
        {
            header.Add(Flood);
        }

        return header;
    }
    private List<string> BuildGcodeRamp()
    {
        var ramp1 = _toolDiameter;
        var ramp2 = _toolDiameter / 4;
        var rampReturn = ramp1 + ramp2;
        var depth = _safeHeight + _depth;
        var formattedDepth = $"{depth:f4}";
        var ramping = new List<string>
        {
            $";Ramping into Stock",
            $"{AbsolutePos}",
            $"{TravelFeedRate}X{ramp1}Z-{formattedDepth}",
            $"{TravelFeedRate}X{ramp2}",
            $"{TravelFeedRate}X-{rampReturn}",
            $"{IncrementalPos}",
        };
        return ramping;
    }

    public IEnumerable<string> BuildSurfaceLayer()
    {
        var overlapMeasurement = _toolDiameter * _overLapPercent;
        //_width += overlapMeasurement;
        _length += overlapMeasurement;
        var lines = _width / _toolDiameter;
        double y = 0;
        var layer = 1;
        var gcodeList = new List<string> { $";Layer{layer}" };
        var depth =  _depth;
        var formattedDepth = $"{depth:f4}";
        gcodeList.Add($"{TravelFeedRate}Z-{formattedDepth}");
        for (int i = 0; i <= lines; i++)
        {
            if(y>=_width)break;
            gcodeList.Add($"G1 Y{y:f4}");
            gcodeList.Add($"G1 X{_length}");
            y += overlapMeasurement;
            gcodeList.Add($"G1 Y{y:f4}");
            gcodeList.Add("G1 X0");
            y += overlapMeasurement;
        }
        return gcodeList;
    }

    private void WriteFile(List<string> gcodeList)
    {
        var docPath = AppDomain.CurrentDomain.BaseDirectory;
        FilePath = Path.Combine(docPath, "QuickSurface.nc");
        using (var outputFile = new StreamWriter(FilePath))
        {
            foreach (string line in gcodeList)
                outputFile.WriteLine(line);
        }
    }

    private IList<string> BuildShutDown()
    {
        var endList = new List<string>
        {
            ";ShutDown",
            $"{RapidFeedRate}Z{_safeHeight}",
            SpindleOff,
            TurnOffFlood
        };
        return endList;
    }
}

public class TemporaryThreadCulture : IDisposable
{
    CultureInfo _oldCulture;

    public TemporaryThreadCulture(CultureInfo newCulture)
    {
        _oldCulture = CultureInfo.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = newCulture;
    }

    public void Dispose()
    {
        Thread.CurrentThread.CurrentCulture = _oldCulture;
    }
}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Xunit;

public static class TimeZoneInfoTests
{
    private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static String s_strPacific = s_isWindows ? "Pacific Standard Time" : "America/Los_Angeles";
    private static String s_strSydney = s_isWindows ? "AUS Eastern Standard Time" : "Australia/Sydney";
    private static String s_strGMT = s_isWindows ? "GMT Standard Time" : "Europe/London";
    private static String s_strTonga = s_isWindows ? "Tonga Standard Time" : "Pacific/Tongatapu";
    private static String s_strBrasil = s_isWindows ? "E. South America Standard Time" : "America/Sao_Paulo";
    private static String s_strPerth = s_isWindows ? "W. Australia Standard Time" : "Australia/Perth";
    private static String s_strBrasilia = s_isWindows ? "E. South America Standard Time" : "America/Sao_Paulo";
    private static String s_strNairobi = s_isWindows ? "E. Africa Standard Time" : "Africa/Nairobi";
    private static String s_strAmsterdam = s_isWindows ? "W. Europe Standard Time" : "Europe/Berlin";
    private static String s_strRussian = s_isWindows ? "Russian Standard Time" : "Europe/Moscow";
    private static String s_strLibya = s_isWindows ? "Libya Standard Time" : "Africa/Tripoli";

    private static TimeZoneInfo s_myUtc = TimeZoneInfo.Utc;
    private static TimeZoneInfo s_myLocal = TimeZoneInfo.Local;
    private static TimeZoneInfo s_regLocal = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfo.Local.Id); // in case DST is disabled on Local
    private static TimeZoneInfo s_GMTLondon = TimeZoneInfo.FindSystemTimeZoneById(s_strGMT);
    private static TimeZoneInfo s_nairobiTz = TimeZoneInfo.FindSystemTimeZoneById(s_strNairobi);
    private static TimeZoneInfo s_amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById(s_strAmsterdam);

    private static bool s_localIsPST = TimeZoneInfo.Local.Id == s_strPacific;
    private static bool s_regLocalSupportsDST = s_regLocal.SupportsDaylightSavingTime;
    private static bool s_localSupportsDST = TimeZoneInfo.Local.SupportsDaylightSavingTime;

    [Fact]
    public static void TestKind()
    {
        TimeZoneInfo tzi = TimeZoneInfo.Local;
        Assert.Equal(tzi, TimeZoneInfo.Local);
        tzi = TimeZoneInfo.Utc;
        Assert.Equal(tzi, TimeZoneInfo.Utc);
    }

    [Fact]
    public static void TestNames()
    {
        TimeZoneInfo local = TimeZoneInfo.Local;
        TimeZoneInfo utc = TimeZoneInfo.Utc;

        Assert.NotNull(local.DaylightName);
        Assert.NotNull(local.DisplayName);
        Assert.NotNull(local.StandardName);

        Assert.NotNull(utc.DaylightName);
        Assert.NotNull(utc.DisplayName);
        Assert.NotNull(utc.StandardName);
    }

    [Fact]
    public static void ConvertTime()
    {
        TimeZoneInfo local = TimeZoneInfo.Local;
        TimeZoneInfo utc = TimeZoneInfo.Utc;

        DateTime dt = TimeZoneInfo.ConvertTime(DateTime.Today, utc);
        Assert.Equal(DateTime.Today, TimeZoneInfo.ConvertTime(dt, local));

        DateTime today = new DateTime(DateTime.Today.Ticks, DateTimeKind.Utc);
        dt = TimeZoneInfo.ConvertTime(today, local);
        Assert.Equal(today, TimeZoneInfo.ConvertTime(dt, utc));
    }

    [Fact]
    public static void ValidateLibyaTimeZoneTest()
    {
        TimeZoneInfo tripoli;
        // Make sure first the timezone data is updated in the machine as it should include Libya Timezone
        try
        {
            tripoli = TimeZoneInfo.FindSystemTimeZoneById(s_strLibya);
        }
        catch (Exception /* TimeZoneNotFoundException */ )
        {
            // Libya time zone not found
            Console.WriteLine("Warning: Libya time zone is not exist in this machine");
            return;
        }

        var startOf2012 = new DateTime(2012, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOf2011 = startOf2012.AddTicks(-1);

        DateTime libyaLocalTime = TimeZoneInfo.ConvertTime(endOf2011, tripoli);
        DateTime expectResult = new DateTime(2012, 1, 1, 2, 0, 0).AddTicks(-1);
        Assert.True(libyaLocalTime.Equals(expectResult), String.Format("Expected {0} and got {1}", expectResult, libyaLocalTime));
    }

    [Fact]
    [ActiveIssue(2465, PlatformID.AnyUnix)]
    public static void ValidateRussiaTimeZoneTest()
    {
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(s_strRussian);
        var inputUtcDate = new DateTime(2013, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        DateTime russiaTime = TimeZoneInfo.ConvertTime(inputUtcDate, tz);
        DateTime expectResult = new DateTime(2013, 6, 1, 4, 0, 0);
        Assert.True(russiaTime.Equals(expectResult), String.Format("Expected {0} and got {1}", expectResult, russiaTime));

        DateTime dt = new DateTime(2011, 12, 31, 23, 30, 0);
        TimeSpan o = tz.GetUtcOffset(dt);
        Assert.True(o.Equals(TimeSpan.FromHours(4)), String.Format("Expected {0} and got {1}", TimeSpan.FromHours(4), o));
    }

    [Fact]
    public static void ValidateExceptionsTest()
    {
        DateTimeOffset time1 = new DateTimeOffset(2006, 5, 12, 0, 0, 0, TimeSpan.Zero);

        VerifyConvertException<ArgumentNullException>(time1, null);

        //
        // We catch Exception here instead of TimeZoneNotFoundException because TimeZoneNotFoundException is not exposed 
        // in .NET Core
        //

        VerifyConvertException<Exception>(time1, String.Empty);
        VerifyConvertException<Exception>(time1, "    ");
        VerifyConvertException<Exception>(time1, "\0");
        VerifyConvertException<Exception>(time1, "Pacific"); // whole string must match
        VerifyConvertException<Exception>(time1, "Pacific Standard Time Zone"); // no extra characters
        VerifyConvertException<Exception>(time1, " Pacific Standard Time"); // no leading space
        VerifyConvertException<Exception>(time1, "Pacific Standard Time "); // no trailing space
        VerifyConvertException<Exception>(time1, "\0Pacific Standard Time"); // no leading null
        VerifyConvertException<Exception>(time1, "Pacific Standard Time\0"); // no trailing null
        VerifyConvertException<Exception>(time1, "Pacific Standard Time\\  "); // no trailing null
        VerifyConvertException<Exception>(time1, "Pacific Standard Time\\Display");
        VerifyConvertException<Exception>(time1, "Pacific Standard Time\n"); // no trailing newline
        VerifyConvertException<Exception>(time1, new String('a', 256)); // long string
    }

    [Fact]
    public static void NearMinMaxDateTimeOffsetConvertTest()
    {
        VerifyConvert(DateTimeOffset.MaxValue, TimeZoneInfo.Utc.Id, DateTimeOffset.MaxValue);
        VerifyConvert(DateTimeOffset.MaxValue, s_strPacific, new DateTimeOffset(DateTime.MaxValue.AddHours(-8), new TimeSpan(-8, 0, 0)));
        VerifyConvert(DateTimeOffset.MaxValue, s_strSydney, DateTimeOffset.MaxValue);
        VerifyConvert(new DateTimeOffset(DateTime.MaxValue, new TimeSpan(5, 0, 0)), s_strSydney, DateTimeOffset.MaxValue);
        VerifyConvert(new DateTimeOffset(DateTime.MaxValue, new TimeSpan(11, 0, 0)), s_strSydney, new DateTimeOffset(DateTime.MaxValue, new TimeSpan(11, 0, 0)));
        VerifyConvert(DateTimeOffset.MaxValue.AddHours(-11), s_strSydney, new DateTimeOffset(DateTime.MaxValue, new TimeSpan(11, 0, 0)));
        VerifyConvert(DateTimeOffset.MaxValue.AddHours(-11.5), s_strSydney, new DateTimeOffset(DateTime.MaxValue.AddHours(-0.5), new TimeSpan(11, 0, 0)));
        VerifyConvert(new DateTimeOffset(DateTime.MaxValue.AddHours(-5), new TimeSpan(3, 0, 0)), s_strSydney, DateTimeOffset.MaxValue);

        VerifyConvert(DateTimeOffset.MinValue, TimeZoneInfo.Utc.Id, DateTimeOffset.MinValue);
        VerifyConvert(DateTimeOffset.MinValue, s_strSydney, new DateTimeOffset(DateTime.MinValue.AddHours(10), new TimeSpan(10, 0, 0)));
        VerifyConvert(DateTimeOffset.MinValue, s_strPacific, DateTimeOffset.MinValue);
        VerifyConvert(new DateTimeOffset(DateTime.MinValue, new TimeSpan(-3, 0, 0)), s_strPacific, DateTimeOffset.MinValue);
        VerifyConvert(new DateTimeOffset(DateTime.MinValue, new TimeSpan(-8, 0, 0)), s_strPacific, new DateTimeOffset(DateTime.MinValue, new TimeSpan(-8, 0, 0)));
        VerifyConvert(DateTimeOffset.MinValue.AddHours(8), s_strPacific, new DateTimeOffset(DateTime.MinValue, new TimeSpan(-8, 0, 0)));
        VerifyConvert(DateTimeOffset.MinValue.AddHours(8.5), s_strPacific, new DateTimeOffset(DateTime.MinValue.AddHours(0.5), new TimeSpan(-8, 0, 0)));
        VerifyConvert(new DateTimeOffset(DateTime.MinValue.AddHours(5), new TimeSpan(-3, 0, 0)), s_strPacific, new DateTimeOffset(DateTime.MinValue, new TimeSpan(-8, 0, 0)));

        VerifyConvert(DateTime.MaxValue, s_strPacific, s_strSydney, DateTime.MaxValue);
        if (s_isWindows) // [ActiveIssue(2465, PlatformID.AnyUnix)]
        {
            VerifyConvert(DateTime.MaxValue.AddHours(-19), s_strPacific, s_strSydney, DateTime.MaxValue);
        }
        VerifyConvert(DateTime.MaxValue.AddHours(-19.5), s_strPacific, s_strSydney, DateTime.MaxValue.AddHours(-0.5));
        VerifyConvert(DateTime.MinValue, s_strSydney, s_strPacific, DateTime.MinValue);
        if (s_isWindows) // [ActiveIssue(2465, PlatformID.AnyUnix)]
        {
            VerifyConvert(DateTime.MinValue.AddHours(19), s_strSydney, s_strPacific, DateTime.MinValue);
            VerifyConvert(DateTime.MinValue.AddHours(19.5), s_strSydney, s_strPacific, DateTime.MinValue.AddHours(0.5));
        }
    }

    [Fact]
    public static void DateTimeOffsetVariousSystemTimeZonesTest()
    {
        var time1 = new DateTimeOffset(2006, 5, 12, 5, 17, 42, new TimeSpan(-7, 0, 0));
        var time2 = new DateTimeOffset(2006, 5, 12, 22, 17, 42, new TimeSpan(10, 0, 0));
        VerifyConvert(time1, s_strSydney, time2);
        VerifyConvert(time2, s_strPacific, time1);

        time1 = new DateTimeOffset(2006, 3, 14, 9, 47, 12, new TimeSpan(-8, 0, 0));
        time2 = new DateTimeOffset(2006, 3, 15, 4, 47, 12, new TimeSpan(11, 0, 0));
        VerifyConvert(time1, s_strSydney, time2);
        VerifyConvert(time2, s_strPacific, time1);

        time1 = new DateTimeOffset(2006, 11, 5, 1, 3, 0, new TimeSpan(-8, 0, 0));
        time2 = new DateTimeOffset(2006, 11, 5, 20, 3, 0, new TimeSpan(11, 0, 0));
        VerifyConvert(time1, s_strSydney, time2);
        VerifyConvert(time2, s_strPacific, time1);

        time1 = new DateTimeOffset(1987, 1, 1, 2, 3, 0, new TimeSpan(-8, 0, 0));
        time2 = new DateTimeOffset(1987, 1, 1, 21, 3, 0, new TimeSpan(11, 0, 0));
        VerifyConvert(time1, s_strSydney, time2);
        VerifyConvert(time2, s_strPacific, time1);

        time1 = new DateTimeOffset(2001, 5, 12, 5, 17, 42, new TimeSpan(1, 0, 0));
        time2 = new DateTimeOffset(2001, 5, 12, 17, 17, 42, new TimeSpan(13, 0, 0));
        VerifyConvert(time1, s_strTonga, time2);
        VerifyConvert(time2, s_strGMT, time1);
        VerifyConvert(time1, s_strGMT, time1);
        VerifyConvert(time2, s_strTonga, time2);

        time1 = new DateTimeOffset(2003, 3, 30, 5, 19, 20, new TimeSpan(1, 0, 0));
        time2 = new DateTimeOffset(2003, 3, 30, 17, 19, 20, new TimeSpan(13, 0, 0));
        VerifyConvert(time1, s_strTonga, time2);
        VerifyConvert(time2, s_strGMT, time1);
        VerifyConvert(time1, s_strGMT, time1);
        VerifyConvert(time2, s_strTonga, time2);

        time1 = new DateTimeOffset(2003, 3, 30, 1, 20, 1, new TimeSpan(0, 0, 0));
        var time1a = new DateTimeOffset(2003, 3, 30, 2, 20, 1, new TimeSpan(1, 0, 0));
        time2 = new DateTimeOffset(2003, 3, 30, 14, 20, 1, new TimeSpan(13, 0, 0));
        VerifyConvert(time1, s_strTonga, time2);
        VerifyConvert(time1a, s_strTonga, time2);
        VerifyConvert(time2, s_strGMT, time1a);
        VerifyConvert(time1, s_strGMT, time1a);  // invalid hour
        VerifyConvert(time1a, s_strGMT, time1a);
        VerifyConvert(time2, s_strTonga, time2);

        time1 = new DateTimeOffset(2003, 3, 30, 0, 0, 23, new TimeSpan(0, 0, 0));
        time2 = new DateTimeOffset(2003, 3, 30, 13, 0, 23, new TimeSpan(13, 0, 0));
        VerifyConvert(time1, s_strTonga, time2);
        VerifyConvert(time2, s_strGMT, time1);
        VerifyConvert(time1, s_strGMT, time1);
        VerifyConvert(time2, s_strTonga, time2);

        time1 = new DateTimeOffset(2003, 10, 26, 1, 30, 0, new TimeSpan(0)); // ambiguous (STD)
        time1a = new DateTimeOffset(2003, 10, 26, 1, 30, 0, new TimeSpan(1, 0, 0)); // ambiguous (DLT)
        time2 = new DateTimeOffset(2003, 10, 26, 14, 30, 0, new TimeSpan(13, 0, 0));
        VerifyConvert(time1, s_strTonga, time2);
        VerifyConvert(time2, s_strGMT, time1);
        VerifyConvert(time1, s_strGMT, time1);
        VerifyConvert(time2, s_strTonga, time2);
        VerifyConvert(time1a, s_strTonga, time2.AddHours(-1));
        VerifyConvert(time1a, s_strGMT, time1a);

        time1 = new DateTimeOffset(2003, 10, 25, 14, 0, 0, new TimeSpan(1, 0, 0));
        time2 = new DateTimeOffset(2003, 10, 26, 2, 0, 0, new TimeSpan(13, 0, 0));
        VerifyConvert(time1, s_strTonga, time2);
        VerifyConvert(time2, s_strGMT, time1);
        VerifyConvert(time1, s_strGMT, time1);
        VerifyConvert(time2, s_strTonga, time2);

        time1 = new DateTimeOffset(2003, 10, 26, 2, 20, 0, new TimeSpan(0));
        time2 = new DateTimeOffset(2003, 10, 26, 15, 20, 0, new TimeSpan(13, 0, 0));
        VerifyConvert(time1, s_strTonga, time2);
        VerifyConvert(time2, s_strGMT, time1);
        VerifyConvert(time1, s_strGMT, time1);
        VerifyConvert(time2, s_strTonga, time2);

        time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(0));
        time2 = new DateTimeOffset(2003, 10, 26, 16, 0, 1, new TimeSpan(13, 0, 0));
        VerifyConvert(time1, s_strTonga, time2);
        VerifyConvert(time2, s_strGMT, time1);
        VerifyConvert(time1, s_strGMT, time1);
        VerifyConvert(time2, s_strTonga, time2);

        var time3 = new DateTime(2001, 5, 12, 5, 17, 42);
        VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(12));
        VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-12));
        time3 = new DateTime(2003, 3, 30, 5, 19, 20);
        VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(12));
        VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-13));
        time3 = new DateTime(2003, 3, 30, 1, 20, 1);
        VerifyConvertException<ArgumentException>(time3, s_strGMT, s_strTonga); // invalid time
        VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-13));
        time3 = new DateTime(2003, 3, 30, 0, 0, 23);
        VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(13));
        VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-13));
        time3 = new DateTime(2003, 10, 26, 2, 0, 0); // ambiguous
        VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(13));
        VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-12));
        time3 = new DateTime(2003, 10, 26, 2, 20, 0);
        VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(13)); // ambiguous
        VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-12));
        time3 = new DateTime(2003, 10, 26, 3, 0, 1);
        VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(13));
        VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-12));
    }

    [Fact]
    public static void SameTimeZonesTest()
    {
        var time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(-2, 0, 0));
        VerifyConvert(time1, s_strBrasil, time1);
        time1 = new DateTimeOffset(2006, 5, 12, 5, 17, 42, new TimeSpan(-3, 0, 0));
        VerifyConvert(time1, s_strBrasil, time1);
        time1 = new DateTimeOffset(2006, 3, 28, 9, 47, 12, new TimeSpan(-3, 0, 0));
        VerifyConvert(time1, s_strBrasil, time1);
        time1 = new DateTimeOffset(2006, 11, 5, 1, 3, 0, new TimeSpan(-2, 0, 0));
        VerifyConvert(time1, s_strBrasil, time1);
        time1 = new DateTimeOffset(2006, 10, 15, 2, 30, 0, new TimeSpan(-2, 0, 0));  // invalid
        VerifyConvert(time1, s_strBrasil, time1.ToOffset(new TimeSpan(-3, 0, 0)));
        time1 = new DateTimeOffset(2006, 2, 12, 1, 30, 0, new TimeSpan(-3, 0, 0));  // ambiguous
        VerifyConvert(time1, s_strBrasil, time1);
        time1 = new DateTimeOffset(2006, 2, 12, 1, 30, 0, new TimeSpan(-2, 0, 0));  // ambiguous
        VerifyConvert(time1, s_strBrasil, time1);

        time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(-8, 0, 0));
        VerifyConvert(time1, s_strPacific, time1);
        time1 = new DateTimeOffset(2006, 5, 12, 5, 17, 42, new TimeSpan(-7, 0, 0));
        VerifyConvert(time1, s_strPacific, time1);
        time1 = new DateTimeOffset(2006, 3, 28, 9, 47, 12, new TimeSpan(-8, 0, 0));
        VerifyConvert(time1, s_strPacific, time1);
        time1 = new DateTimeOffset(2006, 11, 5, 1, 3, 0, new TimeSpan(-8, 0, 0));
        VerifyConvert(time1, s_strPacific, time1);

        time1 = new DateTimeOffset(1964, 6, 19, 12, 45, 10, new TimeSpan(0));
        VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1);
        time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(-8, 0, 0));
        VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.ToUniversalTime());
        time1 = new DateTimeOffset(2006, 3, 28, 9, 47, 12, new TimeSpan(-3, 0, 0));
        VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.ToUniversalTime());
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void NearMinMaxDateTimeConvertTest()
    {
        DateTime time1 = new DateTime(2006, 5, 12);

        DateTime utcMaxValue = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        VerifyConvert(utcMaxValue, s_strSydney, DateTime.MaxValue);
        VerifyConvert(utcMaxValue.AddHours(-11), s_strSydney, DateTime.MaxValue);
        VerifyConvert(utcMaxValue.AddHours(-11.5), s_strSydney, DateTime.MaxValue.AddHours(-0.5));
        VerifyConvert(utcMaxValue, s_strPacific, DateTime.MaxValue.AddHours(-8));
        DateTime utcMinValue = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        VerifyConvert(utcMinValue, s_strPacific, DateTime.MinValue);
        VerifyConvert(utcMinValue.AddHours(8), s_strPacific, DateTime.MinValue);
        VerifyConvert(utcMinValue.AddHours(8.5), s_strPacific, DateTime.MinValue.AddHours(0.5));
        VerifyConvert(utcMinValue, s_strSydney, DateTime.MinValue.AddHours(11));
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void DateTimeVariousSystemTimeZonesTest()
    {
        var time1utc = new DateTime(2006, 5, 12, 5, 17, 42, DateTimeKind.Utc);
        var time1 = new DateTime(2006, 5, 12, 5, 17, 42);
        VerifyConvert(time1utc, s_strPacific, time1.AddHours(-7));
        VerifyConvert(time1utc, s_strSydney, time1.AddHours(10));
        VerifyConvert(time1utc, s_strGMT, time1.AddHours(1));
        VerifyConvert(time1utc, s_strTonga, time1.AddHours(13));
        time1utc = new DateTime(2006, 3, 28, 9, 47, 12, DateTimeKind.Utc);
        time1 = new DateTime(2006, 3, 28, 9, 47, 12);
        VerifyConvert(time1utc, s_strPacific, time1.AddHours(-8));
        VerifyConvert(time1utc, s_strSydney, time1.AddHours(10));
        time1utc = new DateTime(2006, 11, 5, 1, 3, 0, DateTimeKind.Utc);
        time1 = new DateTime(2006, 11, 5, 1, 3, 0);
        VerifyConvert(time1utc, s_strPacific, time1.AddHours(-8));
        VerifyConvert(time1utc, s_strSydney, time1.AddHours(11));
        time1utc = new DateTime(1987, 1, 1, 2, 3, 0, DateTimeKind.Utc);
        time1 = new DateTime(1987, 1, 1, 2, 3, 0);
        VerifyConvert(time1utc, s_strPacific, time1.AddHours(-8));
        VerifyConvert(time1utc, s_strSydney, time1.AddHours(11));

        time1utc = new DateTime(2003, 3, 30, 0, 0, 23, DateTimeKind.Utc);
        time1 = new DateTime(2003, 3, 30, 0, 0, 23);
        VerifyConvert(time1utc, s_strGMT, time1);
        time1utc = new DateTime(2003, 3, 30, 2, 0, 24, DateTimeKind.Utc);
        time1 = new DateTime(2003, 3, 30, 2, 0, 24);
        VerifyConvert(time1utc, s_strGMT, time1.AddHours(1));
        time1utc = new DateTime(2003, 3, 30, 5, 19, 20, DateTimeKind.Utc);
        time1 = new DateTime(2003, 3, 30, 5, 19, 20);
        VerifyConvert(time1utc, s_strGMT, time1.AddHours(1));
        time1utc = new DateTime(2003, 10, 26, 2, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2003, 10, 26, 2, 0, 0); // ambiguous
        VerifyConvert(time1utc, s_strGMT, time1);
        time1utc = new DateTime(2003, 10, 26, 2, 20, 0, DateTimeKind.Utc);
        time1 = new DateTime(2003, 10, 26, 2, 20, 0);
        VerifyConvert(time1utc, s_strGMT, time1); // ambiguous
        time1utc = new DateTime(2003, 10, 26, 3, 0, 1, DateTimeKind.Utc);
        time1 = new DateTime(2003, 10, 26, 3, 0, 1);
        VerifyConvert(time1utc, s_strGMT, time1);

        time1utc = new DateTime(2005, 3, 30, 0, 0, 23, DateTimeKind.Utc);
        time1 = new DateTime(2005, 3, 30, 0, 0, 23);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));

        time1 = new DateTime(2006, 5, 12, 5, 17, 42);
        VerifyConvert(time1, s_strPacific, s_strSydney, time1.AddHours(17));
        VerifyConvert(time1, s_strSydney, s_strPacific, time1.AddHours(-17));
        time1 = new DateTime(2006, 3, 28, 9, 47, 12);
        VerifyConvert(time1, s_strPacific, s_strSydney, time1.AddHours(18));
        VerifyConvert(time1, s_strSydney, s_strPacific, time1.AddHours(-18));
        time1 = new DateTime(2006, 11, 5, 1, 3, 0);
        VerifyConvert(time1, s_strPacific, s_strSydney, time1.AddHours(19));
        VerifyConvert(time1, s_strSydney, s_strPacific, time1.AddHours(-19));
        time1 = new DateTime(1987, 1, 1, 2, 3, 0);
        VerifyConvert(time1, s_strPacific, s_strSydney, time1.AddHours(19));
        VerifyConvert(time1, s_strSydney, s_strPacific, time1.AddHours(-19));
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void PerthRulesTest()
    {
        var time1utc = new DateTime(2005, 12, 31, 15, 59, 59, DateTimeKind.Utc);
        var time1 = new DateTime(2005, 12, 31, 15, 59, 59);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
        time1utc = new DateTime(2005, 12, 31, 16, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2005, 12, 31, 16, 0, 0);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
        time1utc = new DateTime(2005, 12, 31, 16, 30, 0, DateTimeKind.Utc);
        time1 = new DateTime(2005, 12, 31, 16, 30, 0);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
        time1utc = new DateTime(2005, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        time1 = new DateTime(2005, 12, 31, 23, 59, 59);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
        time1utc = new DateTime(2006, 1, 1, 0, 1, 1, DateTimeKind.Utc);
        time1 = new DateTime(2006, 1, 1, 0, 1, 1);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));

        // 2006 rule in effect
        time1utc = new DateTime(2006, 5, 12, 2, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2006, 5, 12, 2, 0, 0);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));

        // begin dst
        time1utc = new DateTime(2006, 11, 30, 17, 59, 59, DateTimeKind.Utc);
        time1 = new DateTime(2006, 11, 30, 17, 59, 59);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
        time1utc = new DateTime(2006, 11, 30, 18, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2006, 12, 1, 2, 0, 0);
        VerifyConvert(time1utc, s_strPerth, time1);
        // ambiguous time between rules
        // this is not ideal, but the way it works
        time1utc = new DateTime(2006, 12, 31, 15, 59, 59, DateTimeKind.Utc);
        time1 = new DateTime(2006, 12, 31, 15, 59, 59);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));

        // 2007 rule
        time1utc = new DateTime(2006, 12, 31, 20, 1, 2, DateTimeKind.Utc);
        time1 = new DateTime(2006, 12, 31, 20, 1, 2);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
        // end dst
        time1utc = new DateTime(2007, 3, 24, 16, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2007, 3, 24, 16, 0, 0);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
        time1utc = new DateTime(2007, 3, 24, 17, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2007, 3, 24, 17, 0, 0);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
        time1utc = new DateTime(2007, 3, 24, 18, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2007, 3, 24, 18, 0, 0);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
        time1utc = new DateTime(2007, 3, 24, 19, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2007, 3, 24, 19, 0, 0);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
        // begin dst
        time1utc = new DateTime(2008, 10, 25, 17, 59, 59, DateTimeKind.Utc);
        time1 = new DateTime(2008, 10, 25, 17, 59, 59);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
        time1utc = new DateTime(2008, 10, 25, 18, 0, 0, DateTimeKind.Utc);
        time1 = new DateTime(2008, 10, 25, 18, 0, 0);
        VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
    }

    [Fact]
    public static void UtcToUtcTest()
    {
        var time1utc = new DateTime(2003, 3, 30, 0, 0, 23, DateTimeKind.Utc);
        VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
        time1utc = new DateTime(2003, 3, 30, 2, 0, 24, DateTimeKind.Utc);
        VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
        time1utc = new DateTime(2003, 3, 30, 5, 19, 20, DateTimeKind.Utc);
        VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
        time1utc = new DateTime(2003, 10, 26, 2, 0, 0, DateTimeKind.Utc);
        VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
        time1utc = new DateTime(2003, 10, 26, 2, 20, 0, DateTimeKind.Utc);
        VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
        time1utc = new DateTime(2003, 10, 26, 3, 0, 1, DateTimeKind.Utc);
        VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
    }

    [Fact]
    public static void UtcToLocalTest()
    {
        if (s_localIsPST)
        {
            var time1 = new DateTime(2006, 4, 2, 1, 30, 0);
            var time1utc = new DateTime(2006, 4, 2, 1, 30, 0, DateTimeKind.Utc);
            VerifyConvert(time1utc.Subtract(s_regLocal.GetUtcOffset(time1utc)), TimeZoneInfo.Local.Id, time1);

            // Converts to "Pacific Standard Time" not actual Local, so historical rules are always respected
            int delta = s_regLocalSupportsDST ? 1 : 0;
            time1 = new DateTime(2006, 4, 2, 1, 30, 0); // no DST (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2006, 4, 2, 3, 30, 0); // DST (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2006, 10, 29, 0, 30, 0);  // DST (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2006, 10, 29, 1, 30, 0);  // ambiguous hour (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2006, 10, 29, 2, 30, 0);  // no DST (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);

            // 2007 rule
            time1 = new DateTime(2007, 3, 11, 1, 30, 0); // no DST (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2007, 3, 11, 3, 30, 0); // DST (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2007, 11, 4, 0, 30, 0);  // DST (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2007, 11, 4, 1, 30, 0);  // ambiguous hour (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2007, 11, 4, 2, 30, 0);  // no DST (U.S. Pacific)
            VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
        }
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void LocalToSystemTest()
    {
        var time1 = new DateTime(2006, 5, 12, 5, 17, 42);
        var time1local = new DateTime(2006, 5, 12, 5, 17, 42, DateTimeKind.Local);
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strSydney, time1.Subtract(localOffset).AddHours(10));
        VerifyConvert(time1local, s_strSydney, time1.Subtract(localOffset).AddHours(10));
        VerifyConvert(time1, s_strPacific, time1.Subtract(localOffset).AddHours(-7));
        VerifyConvert(time1local, s_strPacific, time1.Subtract(localOffset).AddHours(-7));
        time1 = new DateTime(2006, 3, 28, 9, 47, 12);
        time1local = new DateTime(2006, 3, 28, 9, 47, 12, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strSydney, time1.Subtract(localOffset).AddHours(10));
        VerifyConvert(time1local, s_strSydney, time1.Subtract(localOffset).AddHours(10));
        VerifyConvert(time1, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
        VerifyConvert(time1local, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
        time1 = new DateTime(2006, 11, 5, 1, 3, 0);
        time1local = new DateTime(2006, 11, 5, 1, 3, 0, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strSydney, time1.Subtract(localOffset).AddHours(11));
        VerifyConvert(time1local, s_strSydney, time1.Subtract(localOffset).AddHours(11));
        VerifyConvert(time1, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
        VerifyConvert(time1local, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
        time1 = new DateTime(1987, 1, 1, 2, 3, 0);
        time1local = new DateTime(1987, 1, 1, 2, 3, 0, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strSydney, time1.Subtract(localOffset).AddHours(11));
        VerifyConvert(time1local, s_strSydney, time1.Subtract(localOffset).AddHours(11));
        VerifyConvert(time1, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
        VerifyConvert(time1local, s_strPacific, time1.Subtract(localOffset).AddHours(-8));

        time1 = new DateTime(2001, 5, 12, 5, 17, 42);
        time1local = new DateTime(2001, 5, 12, 5, 17, 42, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        var gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        time1 = new DateTime(2003, 1, 30, 5, 19, 20);
        time1local = new DateTime(2003, 1, 30, 5, 19, 20, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        time1 = new DateTime(2003, 1, 30, 3, 20, 1);
        time1local = new DateTime(2003, 1, 30, 3, 20, 1, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        time1 = new DateTime(2003, 1, 30, 0, 0, 23);
        time1local = new DateTime(2003, 1, 30, 0, 0, 23, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        time1 = new DateTime(2003, 11, 26, 0, 0, 0);
        time1local = new DateTime(2003, 11, 26, 0, 0, 0, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        time1 = new DateTime(2003, 11, 26, 6, 20, 0);
        time1local = new DateTime(2003, 11, 26, 6, 20, 0, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        time1 = new DateTime(2003, 11, 26, 3, 0, 1);
        time1local = new DateTime(2003, 11, 26, 3, 0, 1, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void LocalToLocalTest()
    {
        if (s_localIsPST)
        {
            var time1 = new DateTime(1964, 6, 19, 12, 45, 10);
            VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));

            int delta = TimeZoneInfo.Local.Equals(s_regLocal) ? 0 : 1;
            time1 = new DateTime(2007, 3, 11, 1, 0, 0);  // just before DST transition
            VerifyConvert(time1, TimeZoneInfo.Local.Id, time1);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2007, 3, 11, 2, 0, 0);  // invalid (U.S. Pacific)
            if (s_localSupportsDST)
            {
                VerifyConvertException<ArgumentException>(time1, TimeZoneInfo.Local.Id);
                VerifyConvertException<ArgumentException>(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id);
            }
            else
            {
                VerifyConvert(time1, TimeZoneInfo.Local.Id, time1.AddHours(delta));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1.AddHours(delta));
            }
            time1 = new DateTime(2007, 3, 11, 3, 0, 0);  // just after DST transition (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Local.Id, time1.AddHours(delta));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1.AddHours(delta));
            time1 = new DateTime(2007, 11, 4, 0, 30, 0);  // just before DST transition (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Local.Id, time1.AddHours(delta));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1.AddHours(delta));
            time1 = (new DateTime(2007, 11, 4, 1, 30, 0, DateTimeKind.Local)).ToUniversalTime().AddHours(-1).ToLocalTime();  // DST half of repeated hour (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Local.Id, time1.AddHours(delta), DateTimeKind.Unspecified);
            time1 = new DateTime(2007, 11, 4, 1, 30, 0);  // ambiguous (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Local.Id, time1);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1);
            time1 = new DateTime(2007, 11, 4, 2, 30, 0);  // just after DST transition (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Local.Id, time1);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1);

            time1 = new DateTime(2004, 4, 4, 0, 0, 0);
            VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            time1 = new DateTime(2004, 4, 4, 4, 0, 0);
            VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            time1 = new DateTime(2004, 10, 31, 0, 30, 0);
            VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            time1 = (new DateTime(2004, 10, 31, 1, 30, 0, DateTimeKind.Local)).ToUniversalTime().AddHours(-1).ToLocalTime();
            VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal), DateTimeKind.Unspecified);
            time1 = new DateTime(2004, 10, 31, 1, 30, 0);
            VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            time1 = new DateTime(2004, 10, 31, 3, 0, 0);
            VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
        }
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public static void LocalToUtcTest()
    {
        var time1 = new DateTime(1964, 6, 19, 12, 45, 10);
        VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc);
        VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc);
        // invalid/ambiguous times in Local
        time1 = new DateTime(2006, 5, 12, 7, 34, 59);
        VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc);
        VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc);

        if (s_localIsPST)
        {
            int delta = s_localSupportsDST ? 1 : 0;
            time1 = new DateTime(2006, 4, 2, 1, 30, 0); // no DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            time1 = new DateTime(2006, 4, 2, 2, 30, 0); // invalid hour (U.S. Pacific)
            if (s_localSupportsDST)
            {
                VerifyConvertException<ArgumentException>(time1, TimeZoneInfo.Utc.Id);
                VerifyConvertException<ArgumentException>(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id);
            }
            else
            {
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            }
            time1 = new DateTime(2006, 4, 2, 3, 30, 0); // DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            time1 = new DateTime(2006, 10, 29, 0, 30, 0);  // DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - first time, DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - second time, standard (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            time1 = new DateTime(2006, 10, 29, 1, 30, 0);  // ambiguous hour - unspecified, assume standard (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            time1 = new DateTime(2006, 10, 29, 2, 30, 0);  // no DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);

            // 2007 rule
            time1 = new DateTime(2007, 3, 11, 1, 30, 0); // no DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            time1 = new DateTime(2007, 3, 11, 2, 30, 0); // invalid hour (U.S. Pacific)
            if (s_localSupportsDST)
            {
                VerifyConvertException<ArgumentException>(time1, TimeZoneInfo.Utc.Id);
                VerifyConvertException<ArgumentException>(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id);
            }
            else
            {
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            }
            time1 = new DateTime(2007, 3, 11, 3, 30, 0); // DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            time1 = new DateTime(2007, 11, 4, 0, 30, 0);  // DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - first time, DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
            time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - second time, standard (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            time1 = new DateTime(2007, 11, 4, 1, 30, 0);  // ambiguous hour - unspecified, assume standard (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            time1 = new DateTime(2007, 11, 4, 2, 30, 0);  // no DST (U.S. Pacific)
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
        }
    }

    [Fact]
    public static void DtKindTest()
    {
        VerifyConvertException<ArgumentException>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Utc), s_strPacific, s_strSydney);
        VerifyConvertException<ArgumentException>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Utc), s_strSydney, s_strPacific);
        VerifyConvert(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Utc), "UTC", s_strSydney, new DateTime(2006, 2, 13, 16, 37, 48)); // DCR 24267
        VerifyConvert(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Utc), TimeZoneInfo.Utc.Id, s_strSydney, new DateTime(2006, 2, 13, 16, 37, 48)); // DCR 24267
        VerifyConvert(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Unspecified), TimeZoneInfo.Local.Id, s_strSydney, new DateTime(2006, 2, 13, 5, 37, 48).AddHours(11).Subtract(s_regLocal.GetUtcOffset(new DateTime(2006, 2, 13, 5, 37, 48)))); // DCR 24267
        if (TimeZoneInfo.Local.Id != s_strSydney)
        {
            VerifyConvertException<ArgumentException>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Local), s_strSydney, s_strPacific);
        }
        VerifyConvertException<ArgumentException>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Local), "UTC", s_strPacific);
        VerifyConvertException<Exception>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Local), "Local", s_strPacific);
    }

    [Fact]
    public static void MiscUtcTests()
    {
        VerifyConvert(new DateTime(2003, 4, 6, 1, 30, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 4, 6, 1, 30, 0), DateTimeKind.Utc));
        VerifyConvert(new DateTime(2003, 4, 6, 2, 30, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 4, 6, 2, 30, 0), DateTimeKind.Utc));
        VerifyConvert(new DateTime(2003, 10, 26, 1, 30, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 10, 26, 1, 30, 0), DateTimeKind.Utc));
        VerifyConvert(new DateTime(2003, 10, 26, 2, 30, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 10, 26, 2, 30, 0), DateTimeKind.Utc));
        VerifyConvert(new DateTime(2003, 8, 4, 12, 0, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 8, 4, 12, 0, 0), DateTimeKind.Utc));

        // Round trip

        VerifyRoundTrip(new DateTime(2003, 8, 4, 12, 0, 0, DateTimeKind.Utc), "UTC", TimeZoneInfo.Local.Id);
        VerifyRoundTrip(new DateTime(1929, 3, 9, 23, 59, 59, DateTimeKind.Utc), "UTC", TimeZoneInfo.Local.Id);
        VerifyRoundTrip(new DateTime(2000, 2, 28, 23, 59, 59, DateTimeKind.Utc), "UTC", TimeZoneInfo.Local.Id);
        VerifyRoundTrip(DateTime.UtcNow, "UTC", TimeZoneInfo.Local.Id);

        var time1 = new DateTime(2006, 5, 12, 7, 34, 59);
        VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc));
        VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc));

        if (s_localIsPST)
        {
            int delta = s_localSupportsDST ? 1 : 0;
            time1 = new DateTime(2006, 4, 2, 1, 30, 0); // no DST (U.S. Pacific)
            VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
            time1 = new DateTime(2006, 4, 2, 2, 30, 0); // invalid hour (U.S. Pacific)
            if (s_localSupportsDST)
            {
                VerifyConvertException<ArgumentException>(time1, "UTC");
                VerifyConvertException<ArgumentException>(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC");
            }
            else
            {
                VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
            }
            time1 = new DateTime(2006, 4, 2, 3, 30, 0); // DST (U.S. Pacific)
            VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
            time1 = new DateTime(2006, 10, 29, 0, 30, 0);  // DST (U.S. Pacific)
            VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
            time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - first time, DST (U.S. Pacific)
            VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
            time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - second time, standard (U.S. Pacific)
            VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
            time1 = new DateTime(2006, 10, 29, 1, 30, 0);  // ambiguous hour - unspecified, assume standard (U.S. Pacific)
            VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
            time1 = new DateTime(2006, 10, 29, 2, 30, 0);  // no DST (U.S. Pacific)
            VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
        }
    }

    [Fact]
    public static void BrasiliaTests()
    {
        var time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(-2, 0, 0));
        VerifyConvert(time1, s_strBrasilia, time1);
        time1 = new DateTimeOffset(2006, 5, 12, 5, 17, 42, new TimeSpan(-3, 0, 0));
        VerifyConvert(time1, s_strBrasilia, time1);
        time1 = new DateTimeOffset(2006, 3, 28, 9, 47, 12, new TimeSpan(-3, 0, 0));
        VerifyConvert(time1, s_strBrasilia, time1);
        time1 = new DateTimeOffset(2006, 11, 5, 1, 3, 0, new TimeSpan(-2, 0, 0));
        VerifyConvert(time1, s_strBrasilia, time1);
        time1 = new DateTimeOffset(2006, 10, 15, 2, 30, 0, new TimeSpan(-2, 0, 0));  // invalid
        VerifyConvert(time1, s_strBrasilia, time1.ToOffset(new TimeSpan(-3, 0, 0)));
        time1 = new DateTimeOffset(2006, 2, 12, 1, 30, 0, new TimeSpan(-3, 0, 0));  // ambiguous
        VerifyConvert(time1, s_strBrasilia, time1);
        time1 = new DateTimeOffset(2006, 2, 12, 1, 30, 0, new TimeSpan(-2, 0, 0));  // ambiguous
        VerifyConvert(time1, s_strBrasilia, time1);
    }

    [Fact]
    public static void TongaTests()
    {
        var time1 = new DateTime(2006, 5, 12, 5, 17, 42, DateTimeKind.Utc);
        VerifyConvert(time1, s_strTonga, DateTime.SpecifyKind(time1.AddHours(13), DateTimeKind.Unspecified));

        time1 = new DateTime(2001, 5, 12, 5, 17, 42);
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));

        var time1local = new DateTime(2001, 5, 12, 5, 17, 42, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1local, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));

        time1 = new DateTime(2003, 1, 30, 5, 19, 20);
        time1local = new DateTime(2003, 1, 30, 5, 19, 20, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));
        VerifyConvert(time1local, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));

        time1 = new DateTime(2003, 1, 30, 1, 20, 1);
        time1local = new DateTime(2003, 1, 30, 1, 20, 1, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));
        VerifyConvert(time1local, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));

        time1 = new DateTime(2003, 1, 30, 0, 0, 23);
        time1local = new DateTime(2003, 1, 30, 0, 0, 23, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));

        time1 = new DateTime(2003, 11, 26, 2, 0, 0);
        time1local = new DateTime(2003, 11, 26, 2, 0, 0, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));

        time1 = new DateTime(2003, 11, 26, 2, 20, 0);
        time1local = new DateTime(2003, 11, 26, 2, 20, 0, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));

        time1 = new DateTime(2003, 11, 26, 3, 0, 1);
        time1local = new DateTime(2003, 11, 26, 3, 0, 1, DateTimeKind.Local);
        localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
        VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
    }

    [Fact]
    public static void ThrowOnAmbiguousOffsetsTests()
    {
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32));

        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32, DateTimeKind.Utc));

        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32, DateTimeKind.Local));
    }

    [Fact]
    public static void ThrowOnNairobiAmbiguousOffsetsTests()
    {
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(2006, 1, 15, 7, 15, 23));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(2050, 2, 15, 8, 30, 24));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(1800, 3, 15, 9, 45, 25));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(1400, 4, 15, 10, 00, 26));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(1234, 5, 15, 11, 15, 27));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(4321, 6, 15, 12, 30, 28));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(1111, 7, 15, 13, 45, 29));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(2222, 8, 15, 14, 00, 30));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(9998, 9, 15, 15, 15, 31));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(9997, 10, 15, 16, 30, 32));
    }

    [Fact]
    public static void AmsterdamAmbiguousOffsetsTests()
    {
        //
        // * 00:59:59 Sunday March 26, 2006 in Universal converts to
        //   01:59:59 Sunday March 26, 2006 in Europe/Amsterdam (NO DST)
        //
        // * 01:00:00 Sunday March 26, 2006 in Universal converts to
        //   03:00:00 Sunday March 26, 2006 in Europe/Amsterdam (DST)
        //

        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 00, 03, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 30, 04, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 58, 05, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 15, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 30, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 59, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 00, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 01, 02, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 59, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 03, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 01, 04, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 05, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 06, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 28, 23, 59, 59, DateTimeKind.Utc));

        TimeSpan one = new TimeSpan(+1, 0, 0);
        TimeSpan two = new TimeSpan(+2, 0, 0);

        TimeSpan[] amsterdamAmbiguousOffsets = new TimeSpan[] { one, two };

        //
        // * 00:00:00 Sunday October 29, 2006 in Universal converts to
        //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam  (DST)
        //
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 00, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 01, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 02, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 03, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 04, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 05, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 06, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 07, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 08, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 09, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 10, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 11, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 12, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 13, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 14, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 15, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 30, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 45, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 55, 55, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 00, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 15, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 30, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 45, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 49, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 50, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 51, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 52, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 53, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 54, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 55, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 56, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 57, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 58, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 59, DateTimeKind.Utc), amsterdamAmbiguousOffsets);

        //
        // * 01:00:00 Sunday October 29, 2006 in Universal converts to
        //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam (NO DST)
        //
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 00, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 01, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 02, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 03, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 04, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 05, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 06, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 07, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 08, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 09, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 10, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 11, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 12, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 13, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 14, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 15, 15, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 30, 30, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 45, 45, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 55, 55, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 00, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 15, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 30, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 45, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 49, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 50, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 51, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 52, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 53, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 54, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 55, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 56, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 57, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 58, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 59, DateTimeKind.Utc), amsterdamAmbiguousOffsets);

        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 01, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 02, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 03, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 04, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 05, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 06, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 07, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 01, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 02, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 03, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 04, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 05, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 00, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 00, 01, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 01, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 01, 00, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 02, 00, 00, DateTimeKind.Utc));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 03, 00, 00, DateTimeKind.Utc));

        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 01, 15, 03, 00, 33));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 02, 15, 04, 00, 34));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 01, 02, 00, 35));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 02, 03, 00, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 15, 03, 00, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 25, 03, 00, 36));

        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 45, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 58, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 50));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 59));

        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 30, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 45, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 50, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 59));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 02));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 01, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 02, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 10, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 11, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 15, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 30, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 45, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 50, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 05, 01, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 06, 02, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 07, 03, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 08, 04, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 09, 05, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 10, 06, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 11, 07, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 12, 08, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 13, 09, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 14, 10, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 15, 01, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 16, 02, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 17, 03, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 18, 04, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 19, 05, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 20, 06, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 21, 07, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 22, 08, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 23, 09, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 20, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 01, 36));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 05, 15, 04, 15, 37));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 06, 15, 02, 15, 38));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 07, 15, 01, 15, 39));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 08, 15, 00, 15, 40));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 09, 15, 10, 15, 41));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 15, 08, 15, 42));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 27, 08, 15, 43));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 28, 08, 15, 44));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 45));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 46));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 47));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 48));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 15, 49));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 30, 50));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 45, 51));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 55, 52));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 58, 53));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 54));

        //    March 26, 2006                            October 29, 2006
        // 3AM            4AM                        2AM              3AM
        // |      +1 hr     |                        |       -1 hr      |
        // | <invalid time> |                        | <ambiguous time> |
        //                  *========== DST ========>*
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 00), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 01), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 05), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 09), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 15), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 01, 14), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 02, 12), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 10, 55), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 21, 50), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 32, 40), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 43, 30), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 56, 20), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 57, 10), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 45), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 55), amsterdamAmbiguousOffsets);
        VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 59), amsterdamAmbiguousOffsets);

        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 00));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 01, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 02, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 03, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 04, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 05, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 10, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 04, 10, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 05, 10, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 06, 10, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 07, 10, 01));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 15, 10, 15, 43));
        VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 12, 15, 12, 15, 44));
    }

    [Fact]
    public static void LocalAmbiguousOffsetsTests()
    {
        if (!s_localIsPST)
            return; // Test valid for Pacific TZ only

        TimeSpan[] localOffsets = new TimeSpan[] { new TimeSpan(-7, 0, 0), new TimeSpan(-8, 0, 0) };
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 3, 14, 2, 0, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 3, 14, 2, 0, 0, DateTimeKind.Local)); // use correct rule
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 3, 14, 3, 0, 0, DateTimeKind.Local)); // use correct rule
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 1, 30, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 1, 30, 0, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 2, 0, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 2, 0, 0, DateTimeKind.Local)); // invalid time
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 2, 30, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 2, 30, 0, DateTimeKind.Local)); // invalid time
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 0, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 0, 0, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 30, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 30, 0, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 1, 30, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 1, 30, 0, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 0, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 0, 0, DateTimeKind.Local)); // invalid time
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0, DateTimeKind.Local)); // invalid time
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 3, 0, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 3, 0, 0, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 3, 30, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 3, 30, 0, DateTimeKind.Local));

        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 0, 30, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 0, 30, 0, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 0, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 0, 0, DateTimeKind.Local));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 30, 0));
        VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 30, 0, DateTimeKind.Local));
    }

    [Fact]
    public static void DstTests()
    {
        VerifyDST(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32), false);
        VerifyDST(TimeZoneInfo.Utc, new DateTime(2004, 4, 4, 2, 30, 0, DateTimeKind.Local), false);

        VerifyDST(s_nairobiTz, new DateTime(2007, 3, 11, 2, 30, 0, DateTimeKind.Local), false);
        VerifyDST(s_nairobiTz, new DateTime(2006, 1, 15, 7, 15, 23), false);
        VerifyDST(s_nairobiTz, new DateTime(2050, 2, 15, 8, 30, 24), false);
        VerifyDST(s_nairobiTz, new DateTime(1800, 3, 15, 9, 45, 25), false);
        VerifyDST(s_nairobiTz, new DateTime(1400, 4, 15, 10, 00, 26), false);
        VerifyDST(s_nairobiTz, new DateTime(1234, 5, 15, 11, 15, 27), false);
        VerifyDST(s_nairobiTz, new DateTime(4321, 6, 15, 12, 30, 28), false);
        VerifyDST(s_nairobiTz, new DateTime(1111, 7, 15, 13, 45, 29), false);
        VerifyDST(s_nairobiTz, new DateTime(2222, 8, 15, 14, 00, 30), false);
        VerifyDST(s_nairobiTz, new DateTime(9998, 9, 15, 15, 15, 31), false);
        VerifyDST(s_nairobiTz, new DateTime(9997, 10, 15, 16, 30, 32), false);

        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 00, 03, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 30, 04, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 58, 05, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 00, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 15, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 30, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 59, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 00, 00, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 01, 02, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 59, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 03, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 01, 04, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 05, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 06, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 28, 23, 59, 59, DateTimeKind.Utc), true);

        //
        // * 00:00:00 Sunday October 29, 2006 in Universal converts to
        //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam  (DST)
        //
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 00, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 15, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 30, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 45, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 55, 55, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 00, DateTimeKind.Utc), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 59, DateTimeKind.Utc), true);

        //
        // * 01:00:00 Sunday October 29, 2006 in Universal converts to
        //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam (NO DST)
        //
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 00, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 59, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 07, DateTimeKind.Utc), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 00, 00, DateTimeKind.Utc), false);

        VerifyDST(s_amsterdamTz, new DateTime(2006, 01, 15, 03, 00, 33), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 02, 15, 04, 00, 34), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 01, 02, 00, 35), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 02, 03, 00, 36), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 15, 03, 00, 36), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 25, 03, 00, 36), false);

        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 00), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 36), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 36), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 45, 36), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 36), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 58, 36), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 36), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 50), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 59), false);

        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 36), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 30, 36), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 45, 36), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 50, 36), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 59), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 01), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 02), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 01, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 02, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 10, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 11, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 15, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 30, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 45, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 50, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 05, 01, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 06, 02, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 07, 03, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 08, 04, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 09, 05, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 10, 06, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 11, 07, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 12, 08, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 13, 09, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 14, 10, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 15, 01, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 16, 02, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 17, 03, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 18, 04, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 19, 05, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 20, 06, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 21, 07, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 22, 08, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 23, 09, 00), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 20, 36), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 01, 36), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 05, 15, 04, 15, 37), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 06, 15, 02, 15, 38), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 07, 15, 01, 15, 39), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 08, 15, 00, 15, 40), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 09, 15, 10, 15, 41), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 15, 08, 15, 42), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 27, 08, 15, 43), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 28, 08, 15, 44), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 45), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 46), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 47), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 48), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 15, 49), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 30, 50), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 45, 51), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 55, 52), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 58, 53), true);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 54), true);

        //    March 26, 2006                            October 29, 2006
        // 3AM            4AM                        2AM              3AM
        // |      +1 hr     |                        |       -1 hr      |
        // | <invalid time> |                        | <ambiguous time> |
        //                  *========== DST ========>*
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 00), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 05), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 09), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 15), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 01, 14), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 02, 12), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 10, 55), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 21, 50), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 32, 40), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 43, 30), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 56, 20), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 57, 10), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 45), false);

        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 00), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 01, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 02, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 03, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 04, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 05, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 10, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 04, 10, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 05, 10, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 06, 10, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 07, 10, 01), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 11, 15, 10, 15, 43), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 12, 15, 12, 15, 44), false);
        VerifyDST(s_amsterdamTz, new DateTime(2006, 4, 2, 2, 30, 0, DateTimeKind.Local), true);

        if (s_localIsPST)
        {
            VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 0, 0), true);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 0, 0, DateTimeKind.Local), true);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 30, 0), true);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 30, 0, DateTimeKind.Local), true);

            VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 10, 31, 0, 30, 0), true);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 10, 31, 0, 30, 0, DateTimeKind.Local), true);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 1, 30, 0), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 1, 30, 0, DateTimeKind.Local), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 0, 0), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 0, 0, DateTimeKind.Local), false); // invalid time
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0, DateTimeKind.Local), false); // invalid time
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 1, 0, 0), false); // ambiguous
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 1, 0, 0, DateTimeKind.Local), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 1, 30, 0), false); // ambiguous
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 1, 30, 0, DateTimeKind.Local), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 0, 0), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 0, 0, DateTimeKind.Local), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 30, 0), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 30, 0, DateTimeKind.Local), false);
            VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0, DateTimeKind.Local), false);
        }
    }

    [Fact]
    public static void InvalidTimeTests()
    {
        VerifyInv(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31), false);
        VerifyInv(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32), false);

        VerifyInv(s_nairobiTz, new DateTime(2006, 1, 15, 7, 15, 23), false);
        VerifyInv(s_nairobiTz, new DateTime(2050, 2, 15, 8, 30, 24), false);
        VerifyInv(s_nairobiTz, new DateTime(1800, 3, 15, 9, 45, 25), false);
        VerifyInv(s_nairobiTz, new DateTime(1400, 4, 15, 10, 00, 26), false);
        VerifyInv(s_nairobiTz, new DateTime(1234, 5, 15, 11, 15, 27), false);
        VerifyInv(s_nairobiTz, new DateTime(4321, 6, 15, 12, 30, 28), false);
        VerifyInv(s_nairobiTz, new DateTime(1111, 7, 15, 13, 45, 29), false);
        VerifyInv(s_nairobiTz, new DateTime(2222, 8, 15, 14, 00, 30), false);
        VerifyInv(s_nairobiTz, new DateTime(9998, 9, 15, 15, 15, 31), false);
        VerifyInv(s_nairobiTz, new DateTime(9997, 10, 15, 16, 30, 32), false);

        //    March 26, 2006                            October 29, 2006
        // 2AM            3AM                        2AM              3AM
        // |      +1 hr     |                        |       -1 hr      |
        // | <invalid time> |                        | <ambiguous time> |
        //                  *========== DST ========>*

        //
        // * 00:59:59 Sunday March 26, 2006 in Universal converts to
        //   01:59:59 Sunday March 26, 2006 in Europe/Amsterdam (NO DST)
        //
        // * 01:00:00 Sunday March 26, 2006 in Universal converts to
        //   03:00:00 Sunday March 26, 2006 in Europe/Amsterdam (DST)
        //

        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 00, 03, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 30, 04, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 58, 05, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 00, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 15, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 30, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 59, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 00, 00, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 01, 02, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 59, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 03, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 01, 04, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 05, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 06, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 28, 23, 59, 59, DateTimeKind.Utc), false);

        //
        // * 00:00:00 Sunday October 29, 2006 in Universal converts to
        //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam  (DST)
        //
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 00, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 15, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 30, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 45, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 55, 55, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 00, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 59, DateTimeKind.Utc), false);

        //
        // * 01:00:00 Sunday October 29, 2006 in Universal converts to
        //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam (NO DST)
        //
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 00, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 59, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 07, DateTimeKind.Utc), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 00, 00, DateTimeKind.Utc), false);

        VerifyInv(s_amsterdamTz, new DateTime(2006, 01, 15, 03, 00, 33), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 02, 15, 04, 00, 34), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 01, 02, 00, 35), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 02, 03, 00, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 15, 03, 00, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 25, 03, 00, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 25, 03, 00, 59), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 00, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 01, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 30, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 00, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 30, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 50, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 55, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 10), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 20), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 30), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 40), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 50), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 55), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 56), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 57), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 58), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 59), false);

        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 00), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 01), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 10), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 20), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 30), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 36), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 40), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 50), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 01, 00), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 36), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 45, 36), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 36), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 58, 36), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 36), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 50), true);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 59), true);

        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 30, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 45, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 50, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 59), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 02), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 01, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 02, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 10, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 11, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 15, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 30, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 45, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 50, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 05, 01, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 06, 02, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 07, 03, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 08, 04, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 09, 05, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 10, 06, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 11, 07, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 12, 08, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 13, 09, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 14, 10, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 15, 01, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 16, 02, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 17, 03, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 18, 04, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 19, 05, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 20, 06, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 21, 07, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 22, 08, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 23, 09, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 20, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 01, 36), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 05, 15, 04, 15, 37), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 06, 15, 02, 15, 38), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 07, 15, 01, 15, 39), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 08, 15, 00, 15, 40), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 09, 15, 10, 15, 41), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 15, 08, 15, 42), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 27, 08, 15, 43), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 28, 08, 15, 44), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 45), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 46), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 47), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 48), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 15, 49), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 30, 50), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 45, 51), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 55, 52), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 58, 53), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 54), false);

        //    March 26, 2006                            October 29, 2006
        // 3AM            4AM                        2AM              3AM
        // |      +1 hr     |                        |       -1 hr      |
        // | <invalid time> |                        | <ambiguous time> |
        //                  *========== DST ========>*
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 05), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 09), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 15), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 01, 14), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 02, 12), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 10, 55), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 21, 50), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 32, 40), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 43, 30), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 56, 20), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 57, 10), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 45), false);

        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 00), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 01, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 02, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 03, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 04, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 05, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 10, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 04, 10, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 05, 10, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 06, 10, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 07, 10, 01), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 11, 15, 10, 15, 43), false);
        VerifyInv(s_amsterdamTz, new DateTime(2006, 12, 15, 12, 15, 44), false);
    }

    //
    //  Helper Methods
    //

    private static void VerifyConvertException<EXCTYPE>(DateTimeOffset inputTime, String destinationTimeZoneId) where EXCTYPE : Exception
    {
        Assert.ThrowsAny<EXCTYPE>(() =>
        {
            DateTimeOffset dt = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
        });
    }

    private static void VerifyConvertException<EXCTYPE>(DateTime inputTime, String destinationTimeZoneId) where EXCTYPE : Exception
    {
        Assert.ThrowsAny<EXCTYPE>(() =>
        {
            DateTime dt = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
        });
    }

    private static void VerifyConvertException<EXCTYPE>(DateTime inputTime, String sourceTimeZoneId, String destinationTimeZoneId) where EXCTYPE : Exception
    {
        Assert.ThrowsAny<EXCTYPE>(() =>
        {
            DateTime dt = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(sourceTimeZoneId), TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
        });
    }

    private static void VerifyConvert(DateTimeOffset inputTime, String destinationTimeZoneId, DateTimeOffset expectedTime)
    {
        DateTimeOffset returnedTime = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
        Assert.True(returnedTime.Equals(expectedTime), String.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime, returnedTime, inputTime, destinationTimeZoneId));
    }

    private static void VerifyConvert(DateTime inputTime, String destinationTimeZoneId, DateTime expectedTime)
    {
        DateTime returnedTime = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
        Assert.True(returnedTime.Equals(expectedTime), String.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime, returnedTime, inputTime, destinationTimeZoneId));
        Assert.True(expectedTime.Kind == returnedTime.Kind, String.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime.Kind, returnedTime.Kind, inputTime, destinationTimeZoneId));
    }

    private static void VerifyConvert(DateTime inputTime, String destinationTimeZoneId, DateTime expectedTime, DateTimeKind expectedKind)
    {
        DateTime returnedTime = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
        Assert.True(returnedTime.Equals(expectedTime), String.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime, returnedTime, inputTime, destinationTimeZoneId));
        Assert.True(expectedKind == returnedTime.Kind, String.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime.Kind, returnedTime.Kind, inputTime, destinationTimeZoneId));
    }

    private static void VerifyConvert(DateTime inputTime, String sourceTimeZoneId, String destinationTimeZoneId, DateTime expectedTime)
    {
        DateTime returnedTime = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(sourceTimeZoneId), TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
        Assert.True(returnedTime.Equals(expectedTime), String.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', Source TimeZone: {3}, Dest. Time Zone: {4}", expectedTime, returnedTime, inputTime, sourceTimeZoneId, destinationTimeZoneId));
        Assert.True(expectedTime.Kind == returnedTime.Kind, String.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', Source TimeZone: {3}, Dest. Time Zone: {4}", expectedTime.Kind, returnedTime.Kind, inputTime, sourceTimeZoneId, destinationTimeZoneId));
    }

    private static void VerifyRoundTrip(DateTime dt1, String sourceTimeZoneId, String destinationTimeZoneId)
    {
        TimeZoneInfo sourceTzi = TimeZoneInfo.FindSystemTimeZoneById(sourceTimeZoneId);
        TimeZoneInfo destTzi = TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId);

        DateTime dt2 = TimeZoneInfo.ConvertTime(dt1, sourceTzi, destTzi);
        DateTime dt3 = TimeZoneInfo.ConvertTime(dt2, destTzi, sourceTzi);

        Assert.True(dt1.Equals(dt3), String.Format("{0} failed to round trip using source '{1}' and '{2}' zones. wrong result {3}", dt1, sourceTimeZoneId, destinationTimeZoneId, dt3));

        if (sourceTimeZoneId == TimeZoneInfo.Utc.Id)
        {
            Assert.True(dt3.Kind == DateTimeKind.Utc, String.Format("failed to get the right DT Kind after round trip {0} using source TZ {1} and dest TZi {2}", dt1, sourceTimeZoneId, destinationTimeZoneId));
        }
    }

    private static void VerifyAmbiguousOffsetsException<EXCTYPE>(TimeZoneInfo tz, DateTime dt) where EXCTYPE : Exception
    {
        Assert.Throws<EXCTYPE>(() =>
        {
            TimeSpan[] ret = tz.GetAmbiguousTimeOffsets(dt);
        });
    }

    private static void VerifyOffsets(TimeZoneInfo tz, DateTime dt, TimeSpan[] expectedOffsets)
    {
        TimeSpan[] ret = tz.GetAmbiguousTimeOffsets(dt);
        VerifyTimeSpanArray(ret, expectedOffsets, String.Format("Wrong offsets when used {0} with teh zone {1}", dt, tz.Id));
    }

    static public void VerifyTimeSpanArray(TimeSpan[] actual, TimeSpan[] expected, String errorMsg)
    {
        Assert.True(actual != null);
        Assert.True(expected != null);
        Assert.True(actual.Length == expected.Length);

        Array.Sort(expected); // TimeZoneInfo is expected to always return sorted TimeSpan arrays

        for (int i = 0; i < actual.Length; i++)
        {
            Assert.True(actual[i].Equals(expected[i]), errorMsg);
        }
    }

    private static void VerifyDST(TimeZoneInfo tz, DateTime dt, bool expectedDST)
    {
        bool ret = tz.IsDaylightSavingTime(dt);
        Assert.True(ret == expectedDST, String.Format("Test with the zone {0} and date {1} failed", tz.Id, dt));
    }

    private static void VerifyInv(TimeZoneInfo tz, DateTime dt, bool expectedInvalid)
    {
        bool ret = tz.IsInvalidTime(dt);
        Assert.True(expectedInvalid == ret, String.Format("Test with the zone {0} and date {1} failed", tz.Id, dt));
    }
}

using System;
using Emby.Server.Implementations.Library;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class MediaStreamSelectorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetDefaultAudioStreamIndex_EmptyStreams_Null(bool preferDefaultTrack)
    {
        Assert.Null(MediaStreamSelector.GetDefaultAudioStreamIndex(Array.Empty<MediaStream>(), Array.Empty<string>(), preferDefaultTrack));
    }

    [Theory]
    [InlineData(new string[0], false, 1)]
    [InlineData(new string[0], true, 1)]
    [InlineData(new[] { "eng" }, false, 2)]
    [InlineData(new[] { "eng" }, true, 1)]
    [InlineData(new[] { "eng", "fre" }, false, 2)]
    [InlineData(new[] { "fre", "eng" }, false, 1)]
    [InlineData(new[] { "eng", "fre" }, true, 1)]
    public void GetDefaultAudioStreamIndex_PreferredLanguage_SelectsCorrect(string[] preferredLanguages, bool preferDefaultTrack, int expectedIndex)
    {
        var streams = new MediaStream[]
        {
            new()
            {
                Index = 0,
                Type = MediaStreamType.Video,
                IsDefault = true
            },
            new()
            {
                Index = 1,
                Type = MediaStreamType.Audio,
                Language = "fre",
                IsDefault = true
            },
            new()
            {
                Index = 2,
                Type = MediaStreamType.Audio,
                Language = "eng",
                IsDefault = false
            }
        };

        Assert.Equal(expectedIndex, MediaStreamSelector.GetDefaultAudioStreamIndex(streams, preferredLanguages, preferDefaultTrack));
    }

    public static TheoryData<MediaStream, int> GetStreamScore_MediaStream_TestData()
    {
        var data = new TheoryData<MediaStream, int>();

        data.Add(new MediaStream(), 111111);
        data.Add(
            new MediaStream()
            {
                Language = "eng"
            },
            10111111);
        data.Add(
            new MediaStream()
            {
                Language = "fre"
            },
            10011111);
        data.Add(
            new MediaStream()
            {
                IsForced = true
            },
            121111);
        data.Add(
            new MediaStream()
            {
                IsDefault = true
            },
            112111);
        data.Add(
            new MediaStream()
            {
                SupportsExternalStream = true
            },
            111211);
        data.Add(
            new MediaStream()
            {
                IsExternal = true
            },
            111112);
        data.Add(
            new MediaStream()
            {
                Language = "eng",
                IsForced = true,
                IsDefault = true,
                SupportsExternalStream = true,
                IsExternal = true
            },
            10122212);

        return data;
    }

    [Theory]
    [MemberData(nameof(GetStreamScore_MediaStream_TestData))]
    public void GetStreamScore_MediaStream_CorrectScore(MediaStream stream, int expectedScore)
    {
        var languagePref = new[] { "eng", "fre" };

        Assert.Equal(expectedScore, MediaStreamSelector.GetStreamScore(stream, languagePref));
    }

    [Fact]
    public void GetStreamScore_DeprioritizeSpecialTracks_RegularTrackScoresHigher()
    {
        string[] languagePreferences = ["eng"];
        MediaStream regularStream = new()
        {
            Type = MediaStreamType.Audio,
            Language = "eng",
            Title = "English"
        };
        MediaStream commentaryStream = new()
        {
            Type = MediaStreamType.Audio,
            Language = "eng",
            Title = "Director Commentary"
        };

        int regularScore = MediaStreamSelector.GetStreamScore(regularStream, languagePreferences, true);
        int commentaryScore = MediaStreamSelector.GetStreamScore(commentaryStream, languagePreferences, true);

        Assert.True(regularScore > commentaryScore);
    }

    [Fact]
    public void GetStreamScore_DeprioritizeSpecialTracks_HearingImpairedSubtitleScoresLower()
    {
        string[] languagePreferences = ["eng"];
        MediaStream regularStream = new()
        {
            Type = MediaStreamType.Subtitle,
            Language = "eng"
        };
        MediaStream hearingImpairedStream = new()
        {
            Type = MediaStreamType.Subtitle,
            Language = "eng",
            IsHearingImpaired = true
        };

        int regularScore = MediaStreamSelector.GetStreamScore(regularStream, languagePreferences, true);
        int hearingImpairedScore = MediaStreamSelector.GetStreamScore(hearingImpairedStream, languagePreferences, true);

        Assert.True(regularScore > hearingImpairedScore);
    }
}

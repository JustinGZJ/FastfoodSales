using System;
using Stylet;
using DAQ;

public static class PublishMsgEx
{
    public static void PublishError(this IEventAggregator events, string source, string Msg)
    {
        events?.Publish(new MsgItem
        {
            Level = "E",
            Time = DateTime.Now,
            Value = $"{source,15}{Msg}"
        });
    }
    public static void PublishMsg(this IEventAggregator events, string source, string Msg)
    {
        events.Publish(new MsgItem
        {
            Level = "D",
            Time = DateTime.Now,
            Value = $"{source,15}{Msg}"
        });
    }
}

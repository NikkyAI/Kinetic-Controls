using System;
using UnityEngine;

namespace nikkyai.extensions
{
    public static class RichTextExtensions
    {
        public static string ToHex(this Color color) =>
            string.Format(
                "{0:X2}{1:X2}{2:X2}{3:X2}",
                (byte)Mathf.Clamp01(color.r) * 255,
                (byte)Mathf.Clamp01(color.g) * 255,
                (byte)Mathf.Clamp01(color.b) * 255,
                (byte)Mathf.Clamp01(color.a) * 255
            );

        public static string Color(this string message, Color color) =>
            string.Format(
                "<color=#{0:X2}{1:X2}{2:X2}{3:X2}>",
                (byte)Mathf.Clamp01(color.r) * 255,
                (byte)Mathf.Clamp01(color.g) * 255,
                (byte)Mathf.Clamp01(color.b) * 255,
                (byte)Mathf.Clamp01(color.a) * 255
            ) + message + "</color>";

        public static string Color(this string message, RichTextColor color) =>
            $"<color={color}>" + message + "</color>";


        public static string Bold(this string message) =>
            "<b>" + message + "</b>";

        public static string Italics(this string message) =>
            "<i>" + message + "</i>";
    }

    public enum RichTextColor
    {
        aqua, // (same as cyan) 	#00ffffff
        black, // 	#000000ff
        blue, // 	#0000ffff
        brown, // 	#a52a2aff
        cyan, // (same as aqua) 	#00ffffff
        darkblue, // 	#0000a0ff
        fuchsia, // (same as magenta) 	#ff00ffff
        green, // 	#008000ff
        grey, // 	#808080ff
        lightblue, // 	#add8e6ff
        lime, // 	#00ff00ff
        magenta, // (same as fuchsia) 	#ff00ffff
        maroon, // 	#800000ff
        navy, // 	#000080ff
        olive, // 	#808000ff
        orange, // 	#ffa500ff
        purple, // 	#800080ff
        red, // 	#ff0000ff
        silver, // 	#c0c0c0ff
        teal, // 	#008080ff
        white, // 	#ffffffff
        yellow, //   #ffff00ff
    }
}
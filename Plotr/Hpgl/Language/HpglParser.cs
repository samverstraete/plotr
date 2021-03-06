﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TextParsing;

namespace Hpgl.Language
{
    public class HpglParser : TextParser
    {

        public List<HpglItem> Parse(Stream s)
        {
            using (_reader = new StreamReader(s))
            {
                return ParseReader(_reader);
            }
        }

        private List<HpglItem> ParseReader(StreamReader sr)
        {
            var result = new List<HpglItem>();
            while (readCommand())
            {
                result.Add(ParseCommand());
            }
            return result;
        }

        string textTerminator = "\x0d";
        private HpglItem ParseCommand()
        {
            switch (command.ToUpperInvariant())
            {
                case ";":
                    return new Terminator();
                case "PU":
                    return new PenUp() { Points = readPoints() };
                case "PD":
                    return new PenDown() { Points = readPoints() };
                case "PA":
                    return new PenAbsolute() { Points = readPoints() };
                case "PR":
                    return new PenRelative() { Points = readPoints() };
                case "LB":
                    {
                        string text = "";
                        while (!_reader.EndOfStream && !text.EndsWith(textTerminator))
                        {
                            text += peek();
                            read();
                        }
                        text = text.Substring(0, text.Length - textTerminator.Length);
                        return new Label() { Text = text };
                    }
                case "IN":
                    return new Initialization();
                case "SP":
                    {
                        int i;
                        readNumber(out i); 
                        return new SelectPen() { Pen = i};
                    }
                default:
                    return new UnknownCommand() { Command = command };
            }
        }

        string command;
        private bool readCommand()
        {
            do
            {
                while (!_reader.EndOfStream && !(Char.IsLetter(peek()) || peek() == ';'))
                {
                    read();
                }
                if (_reader.EndOfStream)
                    return false;
                command = "" + peek();
                read();
                if (command == ";")
                    return true;
            } while (!Char.IsLetter(peek()));
            command += peek();
            read();
            return true;
        }

        //  {ws}XX{ws}[param[{ws},{ws}param]]{ws}[;]
        private List<HPoint> readPoints()
        {
            var result = new List<HPoint>();
            HPoint pt;
            while (readPoint(out pt))
            {
                result.Add(pt);
                readWhitespaces();
                if (peek() != ',')
                    break;
                read();
                readWhitespaces();
            }
            return result;
        }

        private bool readPoint(out HPoint pt)
        {
            pt = new HPoint();
            int i;
            if (!readNumber(out i))
                return false;
            pt.X = i;
            if (!readWhitespaces())
                return false;
            if (peek() != ',')
                return false;
            read();
            if (!readNumber(out i))
                return false;
            pt.Y = i;
            return true;
        }

        private bool readNumber(out int i)
        {
            i = 0;
            if (!readWhitespaces())
                return false;
            string s = "";
            while (!_reader.EndOfStream && (Char.IsDigit(peek()) || peek()=='-'))
            {
                s += peek();
                read();
            }
            if (_reader.EndOfStream)
                return false;
            return Int32.TryParse(s, out i);
        }

        private bool readWhitespaces()
        {
            while (!_reader.EndOfStream && Char.IsWhiteSpace(peek()))
            {
                read();
            }
            return !_reader.EndOfStream;
        }

    }

}

﻿//===============================================================================
// TinyCsv
//
// https://github.com/grumpydev/TinyCsv
//===============================================================================
// Copyright © 2017 Steven Robbins  All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//===============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TinyCsv
{
    public interface ILineReader : IDisposable
    {
        int? NumberOfLines { get; }

        Task<string> GetNextLine();
    }

    public class StringLineReader : ILineReader
    {
        private readonly string[] _contentLines;
        private int _currentIndex = 0;

        public StringLineReader(string contents)
        {
            _contentLines = Regex.Split(contents, "\r\n|\r|\n");
        }

        public int? NumberOfLines
        {
            get { return this._contentLines.Length; }
        }

        public Task<string> GetNextLine()
        {
            if (this._currentIndex >= this._contentLines.Length)
            {
                return Task.FromResult<string>(null);
            }

            return Task.FromResult(this._contentLines[this._currentIndex++]);
        }

        public void Dispose()
        {
        }
    }

    public class StreamLineReader : ILineReader
    {
        private readonly Encoding _encoding;
        private StreamReader _reader;

        public int? NumberOfLines
        {
            get
            {
                return null;
            }
        }

        public StreamLineReader(Stream input, Encoding encoding = null)
        {
            this._encoding = encoding ?? Encoding.Default;
            this._reader = new StreamReader(input, this._encoding);
        }

        public Task<string> GetNextLine()
        {
            return this._reader.ReadLineAsync();
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_reader != null) _reader.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~StreamLineReader()
        {
            Dispose(false);
        }
    }

    public class FileProcessor
    {
        private readonly ILineReader _reader;
        private readonly ILineProcessor _processor;

        public FileProcessor(ILineReader reader, ILineProcessor processor)
        {
            _reader = reader;
            _processor = processor;
        }

        public int Skip { get; set; }

        public async Task<IEnumerable<IEnumerable<string>>> Process()
        {
            var numberOfLines = this._reader.NumberOfLines;
            var lines = numberOfLines.HasValue ? new List<IEnumerable<string>>(numberOfLines.Value) : new List<IEnumerable<string>>();
            string current = null;

            for (var skipIndex = 0; skipIndex < this.Skip; skipIndex++)
            {
                await this._reader.GetNextLine();
            }

            do
            {
                current = await this._reader.GetNextLine();

                if (current != null)
                {
                    lines.Add(this._processor.Process(current));
                }

            } while (current != null);

            this._reader.Dispose();

            return lines;
        }
    }

    public interface ILineProcessor
    {
        IEnumerable<string> Process(string line);
        string ReplaceBlankValuesWith { get; set; }
        bool ReplaceNullValueWithActualNull { get; set; }
    }

    public class LineProcessor : ILineProcessor
    {
        private static readonly char[] DefaultQuoteCharacters = {'\'', '"'};

        private readonly char _separator;

        private readonly char[] _quoteCharacters;

        public int SkipLines { get; set; }

        public string ReplaceBlankValuesWith { get; set; }

        public bool ReplaceNullValueWithActualNull { get; set; }

        public LineProcessor(char separator, char[] quoteCharacters = null)
        {
            this._separator = separator;
            this._quoteCharacters = quoteCharacters ?? DefaultQuoteCharacters;

            this.ReplaceBlankValuesWith = null;
            this.ReplaceNullValueWithActualNull = false;
        }

        public IEnumerable<string> Process(string line)
        {
            var lineCharacters = line.ToCharArray();
            var currentCharacterIndex = 0;
            var currentValueBuilder = new StringBuilder();
            char? currentQuoteCharacter = null;

            while (currentCharacterIndex < lineCharacters.Length)
            {
                var currentCharacter = lineCharacters[currentCharacterIndex];

                if (this.IsSeparator(currentCharacter) && !currentQuoteCharacter.HasValue)
                {
                    yield return currentValueBuilder.ToString();

                    currentValueBuilder.Clear();

                    currentCharacterIndex++;
                    continue;
                }

                if (this.IsQuote(currentCharacter) && currentValueBuilder.Length == 0)
                {
                    currentQuoteCharacter = currentCharacter;

                    currentCharacterIndex++;
                    continue;
                }

                if (currentQuoteCharacter.HasValue && currentCharacter == currentQuoteCharacter.Value)
                {
                    var next = this.PeekCharacter(lineCharacters, currentCharacterIndex);

                    if (!next.HasValue || IsSeparator(next.Value))
                    {
                        currentQuoteCharacter = null;
                        currentCharacterIndex++;

                        continue;
                    }

                    if (next.Value == currentQuoteCharacter.Value)
                    {
                        currentValueBuilder.Append(currentCharacter);
                        currentCharacterIndex = currentCharacterIndex + 2;

                        continue;
                    }

                    currentValueBuilder.Insert(0, currentQuoteCharacter.Value);
                    currentValueBuilder.Append(currentCharacter);
                    currentQuoteCharacter = null;
                    currentCharacterIndex++;
                    continue;
                }

                currentValueBuilder.Append(currentCharacter);
                currentCharacterIndex++;
            }

            yield return currentValueBuilder.ToString();
        }

        private char? PeekCharacter(char[] lineCharacters, int currentCharacterIndex)
        {
            if (currentCharacterIndex + 1 >= lineCharacters.Length)
            {
                return null;
            }

            return lineCharacters[currentCharacterIndex + 1];
        }

        private bool IsQuote(char currentCharacter)
        {
            return this._quoteCharacters.Contains(currentCharacter);
        }

        private bool IsSeparator(char currentCharacter)
        {
            return currentCharacter == _separator;
        }
    }

    public interface IBuilder
    {
        ILineProcessor LineProcessor { get; }
    }

    public class Builder : IBuilder
    {
        public Builder(ILineProcessor lineProcessor)
        {
            LineProcessor = lineProcessor;
        }

        public ILineProcessor LineProcessor { get; private set; }
    }

    public interface IBuilder<out TReader> : IBuilder
    {
        FileProcessor FileProcessor { get; }
        TReader LineReader { get; }
    }

    public class Builder<TReader> : IBuilder<TReader>
    {
        public Builder(TReader reader, ILineProcessor processor, FileProcessor fileProcessor)
        {
            LineReader = reader;
            LineProcessor = processor;
            FileProcessor = fileProcessor;
        }

        public TReader LineReader { get; private set; }

        public ILineProcessor LineProcessor { get; private set; }
        
        public FileProcessor FileProcessor { get; private set; }
    }

    public static class BuilderExtensions
    {
        public static IBuilder<StringLineReader> FromString(this Builder builder, string source)
        {
            var lineReader = new StringLineReader(source);

            return new Builder<StringLineReader>(lineReader, builder.LineProcessor, new FileProcessor(lineReader, builder.LineProcessor));
        }

        public static IBuilder<StreamLineReader> FromStream(this Builder builder, Stream source)
        {
            var lineReader = new StreamLineReader(source);

            return new Builder<StreamLineReader>(lineReader, builder.LineProcessor, new FileProcessor(lineReader, builder.LineProcessor));
        }

        public static IBuilder<TReader> SkipLines<TReader>(this IBuilder<TReader> builder, int lines)
        {
            builder.FileProcessor.Skip = lines;

            return builder;
        }

        public static IBuilder<TReader> ReplaceBlankValuesWith<TReader>(this IBuilder<TReader> builder, string value)
        {
            builder.LineProcessor.ReplaceBlankValuesWith = value;

            return builder;
        }

        public static IBuilder<TReader> ReplaceNullValueWithActualNull<TReader>(this IBuilder<TReader> builder)
        {
            builder.LineProcessor.ReplaceNullValueWithActualNull = true;

            return builder;
        }

        public static Task<IEnumerable<IEnumerable<string>>> Process<TReader>(this IBuilder<TReader> builder)
        {
            return builder.FileProcessor.Process();
        }
    }

    public static class TinyCsv
    {
        public static Builder CommaSeparated(char[] quoteCharacters = null)
        {
            var lineProcessor = new LineProcessor(',', quoteCharacters);

            return new Builder(lineProcessor);
        }

        public static Builder TabSeparated(char[] quoteCharacters = null)
        {
            var lineProcessor = new LineProcessor('\t', quoteCharacters);

            return new Builder(lineProcessor);
        }
    }
}
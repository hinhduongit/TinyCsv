﻿using System.Linq;
using Should;
using Xunit;

namespace TinyCsv.Tests
{
    public class LineProcessorFixture
    {
        private LineProcessor _processor;

        public LineProcessorFixture()
        {
            this._processor = LineProcessor.Csv;
        }

        [Fact]
        public void Should_handle_basic_line()
        {
            // Given
            var line = "this,that,the other";

            // When
            var result = this._processor.Process(line);

            // Then
            result.SequenceEqual(new [] { "this", "that", "the other" }).ShouldBeTrue();
        }

        [Fact]
        public void Should_handle_empty_elements()
        {
            // Given
            var line = "this,,the other";

            // When
            var result = this._processor.Process(line);

            // Then
            result.SequenceEqual(new[] { "this", "", "the other" }).ShouldBeTrue();
        }

        [Fact]
        public void Should_handle_quotes()
        {
            // Given
            var line = "\"this\",\"that\",'the other'";

            // When
            var result = this._processor.Process(line);

            // Then
            result.SequenceEqual(new[] { "this", "that", "the other" }).ShouldBeTrue();
        }

        [Fact]
        public void Should_handle_commas_in_quotes()
        {
            // Given
            var line = "\"this\",\"that\",'the,other'";

            // When
            var result = this._processor.Process(line);

            // Then
            result.SequenceEqual(new[] { "this", "that", "the,other" }).ShouldBeTrue();
        }

        [Fact]
        public void Should_handle_different_quote_character_in_quotes()
        {
            // Given
            var line = "\"this\",\"that\",'the\"other'";

            // When
            var result = this._processor.Process(line);

            // Then
            result.SequenceEqual(new[] { "this", "that", "the\"other" }).ShouldBeTrue();
        }

        [Fact]
        public void Should_handle_escaped_quotes()
        {
            // Given
            var line = "\"this\",\"that\",'the\'\'other'";

            // When
            var result = this._processor.Process(line);

            // Then
            result.SequenceEqual(new[] { "this", "that", "the'other" }).ShouldBeTrue();
        }

        [Fact]
        public void Should_only_assume_quotes_if_initial_character_is_a_quote()
        {
            // Given
            var line = "this,that,the \"other\"";

            // When
            var result = this._processor.Process(line);

            // Then
            result.SequenceEqual(new[] { "this", "that", "the \"other\"" }).ShouldBeTrue();
        }
    }
}
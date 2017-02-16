﻿using System.Collections.Generic;
using System.Linq;
using Should;
using TinyCsv.Tests.Fakes;
using Xunit;

namespace TinyCsv.Tests
{
    public class FileProcessorFixture
    {
        [Fact]
        public void Should_pass_each_line_to_processor()
        {
            // Given
            var lines = new []{ "One", "Two", "Three" };
            var reader = new FakeLineReader(lines);
            var processor = new FakeLineProcessor();
            var fileProcessor = new FileProcessor(reader, processor);

            // When
            var result = fileProcessor.Process().ToArray();

            // Then
            processor.PassedLines.Count.ShouldEqual(3);
            processor.PassedLines[0].ShouldEqual("One");
            processor.PassedLines[1].ShouldEqual("Two");
            processor.PassedLines[2].ShouldEqual("Three");
        }
    }
}
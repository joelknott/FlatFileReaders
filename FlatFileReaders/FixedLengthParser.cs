using System;
using System.Collections.Generic;
using FlatFileReaders.Properties;
using System.IO;

namespace FlatFileReaders
{
    /// <summary>
    /// Extracts records from a file that has value in fixed-length columns.
    /// </summary>
    public sealed class FixedLengthParser : IParser
    {
        private readonly Stream stream;
        private readonly FixedLengthSchema schema;
        private readonly string recordSeparator;
        private readonly char filler;
        private int recordCount;
        private object[] values;
        private bool endOfFile;
        private bool hasError;
        private bool isDisposed;
        private StreamReader reader;

        /// <summary>
        /// Initializes a new instance of a FixedLengthParser.
        /// </summary>
        /// <param name="fileName">The path of the file containing the records to parse.</param>
        /// <param name="schema">The schema object defining which columns are in each record.</param>
        /// <exception cref="System.ArgumentNullException">The schema is null.</exception>
        public FixedLengthParser(string fileName, FixedLengthSchema schema)
            : this(File.OpenRead(fileName), schema, new FixedLengthParserOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of a FixedLengthParser.
        /// </summary>
        /// <param name="fileName">The path to the file containing the records to parse.</param>
        /// <param name="schema">The schema object defining which columns are in each record.</param>
        /// <param name="options">An object containing settings for configuring the parser.</param>
        /// <exception cref="System.ArgumentNullException">The schema is null.</exception>
        /// <exception cref="System.ArgumentNullException">The options object is null.</exception>
        public FixedLengthParser(string fileName, FixedLengthSchema schema, FixedLengthParserOptions options)
            : this(File.OpenRead(fileName), schema, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of a FixedLengthParser.
        /// </summary>
        /// <param name="stream">A stream containing the records to parse.</param>
        /// <param name="schema">The schema object defining which columns are in each record.</param>
        /// <exception cref="System.ArgumentNullException">The stream is null.</exception>
        /// <exception cref="System.ArgumentNullException">The schema object is null.</exception>
        public FixedLengthParser(Stream stream, FixedLengthSchema schema)
            : this(stream, schema, new FixedLengthParserOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of a FixedLengthParser.
        /// </summary>
        /// <param name="stream">A stream containing the records to parse.</param>
        /// <param name="schema">The schema object defining which columns are in each record.</param>
        /// <param name="options">An object containing settings for configuring the parser.</param>
        /// <exception cref="System.ArgumentNullException">The stream is null.</exception>
        /// <exception cref="System.ArgumentNullException">The schema is null.</exception>
        /// <exception cref="System.ArgumentNullException">The options object is null.</exception>
        public FixedLengthParser(Stream stream, FixedLengthSchema schema, FixedLengthParserOptions options)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            this.stream = stream;
            reader = new StreamReader(stream);
            
            this.schema = schema;
            recordSeparator = options.RecordSeparator;
            filler = options.FillCharacter;
        }

        /// <summary>
        /// Finalizes the FixedLengthParser.
        /// </summary>
        ~FixedLengthParser()
        {
            dispose(false);
        }

        /// <summary>
        /// Releases any resources currently held by the parser.
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private void dispose(bool disposing)
        {
            if (disposing)
            {
                stream.Dispose();
            }
            isDisposed = true;
        }

        /// <summary>
        /// Gets the schema being used by the parser.
        /// </summary>
        /// <returns>The schema being used by the parser.</returns>
        Schema IParser.GetSchema()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("FixedLengthParser");
            }
            return schema.Schema;
        }

        /// <summary>
        /// Reads the next record from the file.
        /// </summary>
        /// <returns>True if the next record was parsed; otherwise, false if all files are read.</returns>
        public bool Read()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("FixedLengthParser");
            }
            if (hasError)
            {
                throw new InvalidOperationException(Resources.ReadingWithErrors);
            }
            if (reader.EndOfStream)
            {
                endOfFile = true;
                return false;
            }
            ++recordCount;
            var rawValues = readNextLine();
            values = schema.ParseValues(rawValues);
            return true;
        }

        private string[] readNextLine()
        {
            var line = getNextLine();

            if (line.Length != schema.TotalWidth)
            {
                hasError = true;
                throw new ParserException(recordCount);
            }
            var widths = schema.ColumnWidths;
            var stringValues = new string[schema.ColumnWidths.Count];
            var offset = 0;

            for (var index = 0; index != stringValues.Length; ++index)
            {
                var width = widths[index];
                stringValues[index] = line.Substring(offset, width).Trim(filler);
                offset += width;
            }

            return stringValues;
        }

        private string getNextLine()
        {
            return reader.ReadLine();
        }

        /// <summary>
        /// Gets the values for the current record.
        /// </summary>
        /// <returns>The values of the current record.</returns>
        public object[] GetValues()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("FixedLengthParser");
            }
            if (hasError)
            {
                throw new InvalidOperationException(Resources.ReadingWithErrors);
            }
            if (recordCount == 0)
            {
                throw new InvalidOperationException(Resources.ReadNotCalled);
            }
            if (endOfFile)
            {
                throw new InvalidOperationException(Resources.NoMoreRecords);
            }
            
            var copy = new object[values.Length];
            Array.Copy(values, copy, values.Length);

            return copy;
        }
    }
}

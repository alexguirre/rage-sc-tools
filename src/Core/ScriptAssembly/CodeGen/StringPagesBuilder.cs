namespace ScTools.ScriptAssembly.CodeGen
{
    using System;
    using System.Collections.Generic;
    using ScTools.GameFiles;

    public class StringPagesBuilder
    {
        private readonly List<byte[]> pages = new List<byte[]>();
        private readonly Dictionary<string, uint> addedStrings = new Dictionary<string, uint>();
        private uint length = 0;

        private uint Add(ReadOnlySpan<byte> chars)
        {
            uint strLength = (uint)chars.Length + 1; // + null terminator

            if (strLength >= Script.MaxPageLength / 2)
            {
                // just a safe threshold for strings of half a page, they could be longer but we don't need them for now
                throw new ArgumentException("string too long");
            }

            uint pageIndex = length >> 14;
            if (pageIndex >= pages.Count)
            {
                AddPage();
            }

            uint offset = length & 0x3FFF;
            byte[] page = pages[(int)pageIndex];

            if (offset + strLength > page.Length)
            {
                // the string doesn't fit in the current page, skip until the next one (page is already zeroed out)
                length += (uint)page.Length - offset;
                pageIndex = length >> 14;
                offset = length & 0x3FFF;
                AddPage();
                page = pages[(int)pageIndex];
            }

            chars.CopyTo(page.AsSpan((int)offset));
            page[offset + chars.Length] = 0; // null terminator
            uint id = length;
            length += strLength;

            return id;
        }

        private uint Add(ReadOnlySpan<char> str)
        {
            const int MaxStackLimit = 0x200;
            int byteCount = System.Text.Encoding.UTF8.GetByteCount(str);
            Span<byte> buffer = byteCount <= MaxStackLimit ? stackalloc byte[byteCount] : new byte[byteCount];
            System.Text.Encoding.UTF8.GetBytes(str, buffer);
            return Add(buffer);
        }

        public uint Add(string str)
        {
            uint offset = Add(str.AsSpan());
            addedStrings.TryAdd(str, offset);
            return offset;
        }

        public uint AddOrGet(string str) => addedStrings.TryGetValue(str, out var offset) ? offset : Add(str);

        public ScriptPage<byte>[] ToPages(out uint stringsLength)
        {
            var p = new ScriptPage<byte>[pages.Count];
            if (pages.Count > 0)
            {
                for (int i = 0; i < pages.Count - 1; i++)
                {
                    p[i] = new ScriptPage<byte> { Data = NewPage() };
                    pages[i].CopyTo(p[i].Data, 0);
                }

                p[^1] = new ScriptPage<byte> { Data = NewPage(length & 0x3FFF) };
                Array.Copy(pages[^1], p[^1].Data, p[^1].Data.Length);
            }

            stringsLength = length;
            return p;
        }

        private void AddPage() => pages.Add(NewPage());
        private byte[] NewPage(uint size = Script.MaxPageLength) => new byte[size];
    }
}

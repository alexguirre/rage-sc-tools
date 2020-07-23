namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    using System;
    using System.Collections.Generic;

    using Antlr4.Runtime.Atn;

    public class Transformer
    {
        private readonly List<ILocationVisitor> visitors = new List<ILocationVisitor>();

        public DisassembledScript Script { get; }
        public IReadOnlyList<ILocationVisitor> Visitors => visitors;

        public Transformer(DisassembledScript script)
        {
            Script = script ?? throw new ArgumentNullException(nameof(script));
        }

        public Transformer With<T>() where T : ILocationVisitor, new()
        {
            visitors.Add(new T());
            return this;
        }

        public void Process()
        {
            foreach (var f in Script.Functions)
            {
                ProcessFunction(f);
            }
        }

        private void ProcessFunction(Function function)
        {
            var loc = function.CodeStart;

            while (loc != null)
            {
                var newLoc = Visit(loc, new VisitContext(Script, function));

                if (loc != newLoc)
                {
                    var prev = loc.Previous;
                    var next = loc.Next;

                    // TODO: should we handle labels here? like if the location replaced by the
                    //       new locations have any labels should we set them in the new locations?
                    //       or do we let the visitors handle this?
                    //       For now, the visitors handle it, see PushStringSimplifier
                    if (newLoc == null) // if null, remove this location
                    {
                        if (prev != null)
                        {
                            prev.Next = next;
                        }
                        else
                        {
                            function.CodeStart = next;
                        }

                        if (next != null)
                        {
                            next.Previous = prev;
                        }
                        else
                        {
                            function.CodeEnd = prev;
                        }
                    }
                    else // insert the new location
                    {
                        // go back as many new locations were prepended
                        var newPrev = newLoc;
                        while (newPrev.Previous != null)
                        {
                            newPrev = newPrev.Previous;
                            prev = prev?.Previous;
                        }

                        // and link it, replacing the old location
                        if (prev != null)
                        {
                            prev.Next = newPrev;
                        }
                        else
                        {
                            // prev is null, we replaced the start of the function
                            function.CodeStart = newPrev;
                        }
                        newPrev.Previous = prev;


                        // go forward as many new locations were appended
                        var newNext = newLoc;
                        while (newNext.Next != null)
                        {
                            newNext = newNext.Next;
                            next = next?.Next;
                        }

                        // and link it, replacing the old location
                        if (next != null)
                        {
                            next.Previous = newNext;
                        }
                        else
                        {
                            // next is null, we replaced the end of the function
                            function.CodeEnd = newNext;
                        }
                        newNext.Next = next;

                        // point loc to the end of the new locations so next iteration doesn't visit the new ones
                        loc = newNext;
                    }
                }
                
                loc = loc.Next;
            }
        }

        private Location Visit(Location loc, VisitContext context)
        {
            foreach (var visitor in Visitors)
            {
                var newLoc = loc.Accept(context, visitor);
                if (newLoc != loc)
                {
                    return newLoc;
                }
            }

            return loc;
        }
    }
}

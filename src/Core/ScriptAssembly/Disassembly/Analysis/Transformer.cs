namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;

    public class Transformer
    {
        private readonly List<Pass> passes = new List<Pass>();

        public DisassembledScript Script { get; }
        public IReadOnlyList<Pass> Passes => passes;

        public Transformer(DisassembledScript script)
        {
            Script = script ?? throw new ArgumentNullException(nameof(script));
        }

        public PassBuilder WithPass() => new PassBuilder(this);

        public void Process()
        {
            foreach (var p in Passes)
            {
                foreach (var f in Script.Functions)
                {
                    ProcessFunction(p, f);
                }
            }
        }

        private void ProcessFunction(Pass pass, Function function)
        {
            var loc = function.CodeStart;

            while (loc != null)
            {
                var newLoc = Visit(pass, loc, new VisitContext(Script, function));

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

        private Location Visit(Pass pass, Location loc, VisitContext context)
        {
            foreach (var visitor in pass.Visitors)
            {
                var newLoc = loc.Accept(context, visitor);
                if (newLoc != loc)
                {
                    return newLoc;
                }
            }

            return loc;
        }

        public sealed class Pass
        {
            public ImmutableArray<ILocationVisitor> Visitors { get; }

            public Pass(IEnumerable<ILocationVisitor> visitors) => Visitors = ImmutableArray.CreateRange(visitors);
        }


        public sealed class PassBuilder
        {
            public Transformer Transformer { get; }
            private readonly List<ILocationVisitor> visitors = new List<ILocationVisitor>();
            private bool isDone = false;

            public PassBuilder(Transformer transformer) => Transformer = transformer;

            public PassBuilder With<T>() where T : ILocationVisitor, new()
            {
                Debug.Assert(!isDone);
                visitors.Add(new T());
                return this;
            }

            public Transformer Done()
            {
                Debug.Assert(!isDone);

                Transformer.passes.Add(new Pass(visitors));
                isDone = true;
                return Transformer;
            }
        }
    }
}

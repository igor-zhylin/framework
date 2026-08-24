// Accord Unit Tests
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Tests.Math
{
    using System;
    using System.Collections.Generic;
    using Accord.Collections;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    [TestFixture]
    public class RedBlackDictionaryTest
    {

        [Test]
        public void DuplicateTest()
        {
            var map = new RedBlackTreeDictionary<int, string>();

            map[1] = "1";

            ClassicAssert.AreEqual("1", map[1]);

            map[2] = "2";

            ClassicAssert.AreEqual("2", map[2]);

            map[1] = "3";

            ClassicAssert.AreEqual("3", map[1]);

            ClassicAssert.AreEqual(2, map.Count);
        }

        [Test]
        public void EmptyTest()
        {
            var map = new RedBlackTreeDictionary<int, string>();

            bool thrown;
            
            thrown = false;
            try { var c = map[1]; }
            catch (KeyNotFoundException) { thrown = true; }
            ClassicAssert.IsTrue(thrown);

            thrown = false;
            try { var min = map.Min();}
            catch (InvalidOperationException) { thrown = true; }
            ClassicAssert.IsTrue(thrown);

            thrown = false;
            try { var max = map.Max();}
            catch (InvalidOperationException) { thrown = true; }
            ClassicAssert.IsTrue(thrown);

            thrown = false;
            try { var next = map.GetNext(0);}
            catch (KeyNotFoundException) { thrown = true; }
            ClassicAssert.IsTrue(thrown);

            thrown = false;
            try { var prev = map.GetPrevious(0); }
            catch (KeyNotFoundException) { thrown = true; }
            ClassicAssert.IsTrue(thrown);

        }

        [Test]
        public void NextPrevTest()
        {
            bool thrown = false; 

            var map = new RedBlackTreeDictionary<int, string>();

            map[0] = "0";
            map[1] = "1";
            map[2] = "2";


            {
                var a = map.GetNext(0);
                ClassicAssert.AreEqual(1, a.Key);
                ClassicAssert.IsTrue(map.TryGetNext(0, out a));
                ClassicAssert.AreEqual(1, a.Key);

                var b = map.GetNext(1);
                ClassicAssert.AreEqual(2, b.Key);
                ClassicAssert.IsTrue(map.TryGetNext(1, out b));
                ClassicAssert.AreEqual(2, b.Key);

                thrown = false;
                try { map.GetNext(2); }
                catch (KeyNotFoundException) { thrown = true; }
                ClassicAssert.IsTrue(thrown);
                ClassicAssert.IsFalse(map.TryGetNext(2, out b));

                thrown = false;
                try { map.GetNext(-1); }
                catch (KeyNotFoundException) { thrown = true; }
                ClassicAssert.IsTrue(thrown);
                ClassicAssert.IsFalse(map.TryGetNext(-1, out b));
            }

            {
                KeyValuePair<int, string> a;

                thrown = false;
                try {  a = map.GetPrevious(0); }
                catch (KeyNotFoundException) { thrown = true; }
                ClassicAssert.IsTrue(thrown);
                ClassicAssert.IsFalse(map.TryGetPrevious(0, out a));
                ClassicAssert.AreEqual(0, a.Key);

                var b = map.GetPrevious(1);
                ClassicAssert.AreEqual(0, b.Key);
                ClassicAssert.IsTrue(map.TryGetPrevious(1, out b));
                ClassicAssert.AreEqual(0, b.Key);

                var c = map.GetPrevious(2);
                ClassicAssert.AreEqual(1, c.Key);
                ClassicAssert.IsTrue(map.TryGetPrevious(2, out b));
                ClassicAssert.AreEqual(1, b.Key);

                thrown = false;
                try { map.GetNext(3); }
                catch (KeyNotFoundException) { thrown = true; }
                ClassicAssert.IsTrue(thrown);
            }
        }

    }
}

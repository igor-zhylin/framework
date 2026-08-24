using System;
using System.Collections.Generic;
using Accord;
using Accord.Math.Geometry;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Accord.Tests.Math
{
    [TestFixture]
    public class SimpleShapeCheckerTest
    {
        private SimpleShapeChecker shapeChecker = new SimpleShapeChecker( );

        private List<IntPoint> idealCicle = new List<IntPoint>( );
        private List<IntPoint> distorredCircle = new List<IntPoint>( );

        private List<IntPoint> square1 = new List<IntPoint>( );
        private List<IntPoint> square1Test = new List<IntPoint>( );
        private List<IntPoint> square2 = new List<IntPoint>( );
        private List<IntPoint> square2Test = new List<IntPoint>( );
        private List<IntPoint> square3 = new List<IntPoint>( );
        private List<IntPoint> rectangle = new List<IntPoint>( );

        private List<IntPoint> triangle1 = new List<IntPoint>( );
        private List<IntPoint> isoscelesTriangle = new List<IntPoint>( );
        private List<IntPoint> equilateralTriangle = new List<IntPoint>( );
        private List<IntPoint> rectangledTriangle = new List<IntPoint>( );

        public SimpleShapeCheckerTest( )
        {
            System.Random rand = new System.Random( );

            // generate sample circles
            double radius = 100;

            for ( int i = 0; i < 360; i += 10 )
            {
                double angle = (double) i / 180 * System.Math.PI;

                // add point to ideal circle
                idealCicle.Add( new IntPoint(
                    (int) ( radius * System.Math.Cos( angle ) ),
                    (int) ( radius * System.Math.Sin( angle ) ) ) );

                // add a bit distortion for distorred cirlce
                double distorredRadius = radius + rand.Next( 7 ) - 3;

                distorredCircle.Add( new IntPoint(
                    (int) ( distorredRadius * System.Math.Cos( angle ) ),
                    (int) ( distorredRadius * System.Math.Sin( angle ) ) ) );
            }

            // generate sample squares
            square1.Add( new IntPoint( 0, 0 ) );
            square1.Add( new IntPoint( 50, 0 ) );
            square1.Add( new IntPoint( 100, 0 ) );
            square1.Add( new IntPoint( 100, 50 ) );
            square1.Add( new IntPoint( 100, 100 ) );
            square1.Add( new IntPoint( 50, 100 ) );
            square1.Add( new IntPoint( 0, 100 ) );
            square1.Add( new IntPoint( 0, 50 ) );

            square2.Add( new IntPoint( 50, 0 ) );
            square2.Add( new IntPoint( 75, 25 ) );
            square2.Add( new IntPoint( 100, 50 ) );
            square2.Add( new IntPoint( 75, 75 ) );
            square2.Add( new IntPoint( 50, 100 ) );
            square2.Add( new IntPoint( 25, 75 ) );
            square2.Add( new IntPoint( 0, 50 ) );
            square2.Add( new IntPoint( 25, 25 ) );

            // these should be obtained as corners
            square1Test.Add( new IntPoint( 0, 0 ) );
            square1Test.Add( new IntPoint( 100, 0 ) );
            square1Test.Add( new IntPoint( 100, 100 ) );
            square1Test.Add( new IntPoint( 0, 100 ) );

            square2Test.Add( new IntPoint( 50, 0 ) );
            square2Test.Add( new IntPoint( 100, 50 ) );
            square2Test.Add( new IntPoint( 50, 100 ) );
            square2Test.Add( new IntPoint( 0, 50 ) );

            // special square, which may look like circle, but should be recognized as circle
            square3.Add( new IntPoint( 50, 0 ) );
            square3.Add( new IntPoint( 100, 50 ) );
            square3.Add( new IntPoint( 50, 100 ) );
            square3.Add( new IntPoint( 0, 50 ) );

            // generate sample rectangle
            rectangle.Add( new IntPoint( 0, 0 ) );
            rectangle.Add( new IntPoint( 50, 0 ) );
            rectangle.Add( new IntPoint( 100, 0 ) );
            rectangle.Add( new IntPoint( 100, 20 ) );
            rectangle.Add( new IntPoint( 100, 40 ) );
            rectangle.Add( new IntPoint( 50, 40 ) );
            rectangle.Add( new IntPoint( 0, 40 ) );
            rectangle.Add( new IntPoint( 0, 20 ) );

            // generate some triangles
            triangle1.Add( new IntPoint( 0, 0 ) );
            triangle1.Add( new IntPoint( 50, 10 ) );
            triangle1.Add( new IntPoint( 100, 20 ) );
            triangle1.Add( new IntPoint( 90, 50 ) );
            triangle1.Add( new IntPoint( 80, 80 ) );
            triangle1.Add( new IntPoint( 40, 40 ) );

            isoscelesTriangle.Add( new IntPoint( 0, 0 ) );
            isoscelesTriangle.Add( new IntPoint( 50, 0 ) );
            isoscelesTriangle.Add( new IntPoint( 100, 0 ) );
            isoscelesTriangle.Add( new IntPoint( 75, 20 ) );
            isoscelesTriangle.Add( new IntPoint( 50, 40 ) );
            isoscelesTriangle.Add( new IntPoint( 25, 20 ) );

            equilateralTriangle.Add( new IntPoint( 0, 0 ) );
            equilateralTriangle.Add( new IntPoint( 50, 0 ) );
            equilateralTriangle.Add( new IntPoint( 100, 0 ) );
            equilateralTriangle.Add( new IntPoint( 75, 43 ) );
            equilateralTriangle.Add( new IntPoint( 50, 86 ) );
            equilateralTriangle.Add( new IntPoint( 25, 43 ) );

            rectangledTriangle.Add( new IntPoint( 0, 0 ) );
            rectangledTriangle.Add( new IntPoint( 20, 0 ) );
            rectangledTriangle.Add( new IntPoint( 40, 0 ) );
            rectangledTriangle.Add( new IntPoint( 20, 50 ) );
            rectangledTriangle.Add( new IntPoint( 0, 100 ) );
            rectangledTriangle.Add( new IntPoint( 0, 50 ) );
        }

        [Test]
        public void IsCircleTest( )
        {
            ClassicAssert.AreEqual( true, shapeChecker.IsCircle( idealCicle ) );
            ClassicAssert.AreEqual( true, shapeChecker.IsCircle( distorredCircle ) );

            ClassicAssert.AreEqual( false, shapeChecker.IsCircle( square1 ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsCircle( square2 ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsCircle( square3 ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsCircle( rectangle ) );

            ClassicAssert.AreEqual( false, shapeChecker.IsCircle( triangle1 ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsCircle( equilateralTriangle ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsCircle( isoscelesTriangle ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsCircle( rectangledTriangle ) );
        }

        [Test]
        public void IsQuadrilateralTest( )
        {
            ClassicAssert.AreEqual( true, shapeChecker.IsQuadrilateral( square1 ) );
            ClassicAssert.AreEqual( true, shapeChecker.IsQuadrilateral( square2 ) );
            ClassicAssert.AreEqual( true, shapeChecker.IsQuadrilateral( square3 ) );
            ClassicAssert.AreEqual( true, shapeChecker.IsQuadrilateral( rectangle ) );

            ClassicAssert.AreEqual( false, shapeChecker.IsQuadrilateral( idealCicle ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsQuadrilateral( distorredCircle ) );

            ClassicAssert.AreEqual( false, shapeChecker.IsQuadrilateral( triangle1 ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsQuadrilateral( equilateralTriangle ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsQuadrilateral( isoscelesTriangle ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsQuadrilateral( rectangledTriangle ) );
        }

        [Test]
        public void CheckQuadrilateralCornersTest( )
        {
            List<IntPoint> corners;

            ClassicAssert.AreEqual( true, shapeChecker.IsQuadrilateral( square1, out corners ) );
            ClassicAssert.AreEqual( 4, corners.Count );
            ClassicAssert.AreEqual( true, CompareShape( corners, square1Test ) );

            ClassicAssert.AreEqual( true, shapeChecker.IsQuadrilateral( square2, out corners ) );
            ClassicAssert.AreEqual( 4, corners.Count );
            ClassicAssert.AreEqual( true, CompareShape( corners, square2Test ) );
        }

        [Test]
        public void IsTriangleTest( )
        {
            ClassicAssert.AreEqual( true, shapeChecker.IsTriangle( triangle1 ) );
            ClassicAssert.AreEqual( true, shapeChecker.IsTriangle( equilateralTriangle ) );
            ClassicAssert.AreEqual( true, shapeChecker.IsTriangle( isoscelesTriangle ) );
            ClassicAssert.AreEqual( true, shapeChecker.IsTriangle( rectangledTriangle ) );

            ClassicAssert.AreEqual( false, shapeChecker.IsTriangle( idealCicle ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsTriangle( distorredCircle ) );

            ClassicAssert.AreEqual( false, shapeChecker.IsTriangle( square1 ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsTriangle( square2 ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsTriangle( square3 ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsTriangle( rectangle ) );
        }

        [Test]
        public void IsConvexPolygon( )
        {
            List<IntPoint> corners;

            ClassicAssert.AreEqual( true, shapeChecker.IsConvexPolygon( triangle1, out corners ) );
            ClassicAssert.AreEqual( 3, corners.Count );
            ClassicAssert.AreEqual( true, shapeChecker.IsConvexPolygon( equilateralTriangle, out corners ) );
            ClassicAssert.AreEqual( 3, corners.Count );
            ClassicAssert.AreEqual( true, shapeChecker.IsConvexPolygon( isoscelesTriangle, out corners ) );
            ClassicAssert.AreEqual( 3, corners.Count );
            ClassicAssert.AreEqual( true, shapeChecker.IsConvexPolygon( rectangledTriangle, out corners ) );
            ClassicAssert.AreEqual( 3, corners.Count );

            ClassicAssert.AreEqual( true, shapeChecker.IsConvexPolygon( square1, out corners ) );
            ClassicAssert.AreEqual( 4, corners.Count );
            ClassicAssert.AreEqual( true, shapeChecker.IsConvexPolygon( square2, out corners ) );
            ClassicAssert.AreEqual( 4, corners.Count );
            ClassicAssert.AreEqual( true, shapeChecker.IsConvexPolygon( square3, out corners ) );
            ClassicAssert.AreEqual( 4, corners.Count );
            ClassicAssert.AreEqual( true, shapeChecker.IsConvexPolygon( rectangle, out corners ) );
            ClassicAssert.AreEqual( 4, corners.Count );

            ClassicAssert.AreEqual( false, shapeChecker.IsConvexPolygon( idealCicle, out corners ) );
            ClassicAssert.AreEqual( false, shapeChecker.IsConvexPolygon( distorredCircle, out corners ) );
        }

        [Test]
        public void CheckShapeTypeTest( )
        {
            ClassicAssert.AreEqual( ShapeType.Circle, shapeChecker.CheckShapeType( idealCicle ) );
            ClassicAssert.AreEqual( ShapeType.Circle, shapeChecker.CheckShapeType( distorredCircle ) );

            ClassicAssert.AreEqual( ShapeType.Quadrilateral, shapeChecker.CheckShapeType( square1 ) );
            ClassicAssert.AreEqual( ShapeType.Quadrilateral, shapeChecker.CheckShapeType( square2 ) );
            ClassicAssert.AreEqual( ShapeType.Quadrilateral, shapeChecker.CheckShapeType( square3 ) );
            ClassicAssert.AreEqual( ShapeType.Quadrilateral, shapeChecker.CheckShapeType( rectangle ) );

            ClassicAssert.AreEqual( ShapeType.Triangle, shapeChecker.CheckShapeType( triangle1 ) );
            ClassicAssert.AreEqual( ShapeType.Triangle, shapeChecker.CheckShapeType( equilateralTriangle ) );
            ClassicAssert.AreEqual( ShapeType.Triangle, shapeChecker.CheckShapeType( isoscelesTriangle ) );
            ClassicAssert.AreEqual( ShapeType.Triangle, shapeChecker.CheckShapeType( rectangledTriangle ) );
        }

        private bool CompareShape( List<IntPoint> shape1, List<IntPoint> shape2 )
        {
            if ( shape1.Count != shape2.Count )
                return false;
            if ( shape1.Count == 0 )
                return true;

            int index = shape1.IndexOf( shape2[0] );

            if ( index == -1 )
                return false;

            index++;

            for ( int i = 1; i < shape2.Count; i++, index++ )
            {
                if ( index >= shape1.Count )
                    index = 0;

                if ( !shape1[index].Equals( shape2[i] ) )
                    return false;
            }

            return true;
        }

        
        [TestCase( PolygonSubType.Unknown, new int[] { 0, 0, 100, 0, 90, 10 } )]     // just a triangle
        [TestCase( PolygonSubType.IsoscelesTriangle, new int[] { 0, 0, 100, 0, 50, 10 } )]
        [TestCase( PolygonSubType.IsoscelesTriangle, new int[] { 0, 0, 100, 0, 50, 200 } )]
        [TestCase( PolygonSubType.EquilateralTriangle, new int[] { 0, 0, 100, 0, 50, 86 } )]
        [TestCase( PolygonSubType.RectangledIsoscelesTriangle, new int[] { 0, 0, 100, 0, 50, 50 } )]
        [TestCase( PolygonSubType.RectangledIsoscelesTriangle, new int[] { 0, 0, 100, 0, 0, 100 } )]
        [TestCase( PolygonSubType.RectangledTriangle, new int[] { 0, 0, 100, 0, 0, 50 } )]
        [TestCase( PolygonSubType.Unknown, new int[] { 0, 0, 100, 0, 90, 50, 10, 70 } )]     // just a quadrilateral
        [TestCase( PolygonSubType.Trapezoid, new int[] { 0, 0, 100, 0, 90, 50, 10, 50 } )]
        [TestCase( PolygonSubType.Trapezoid, new int[] { 0, 0, 100, 0, 90, 50, 0, 50 } )]
        [TestCase( PolygonSubType.Trapezoid, new int[] { 0, 0, 100, 0, 90, 50, 0, 53 } )]    // a bit disformed
        [TestCase( PolygonSubType.Parallelogram, new int[] { 0, 0, 100, 0, 120, 50, 20, 50 } )]
        [TestCase( PolygonSubType.Parallelogram, new int[] { 0, 0, 100, 0, 70, 50, -30, 50 } )]
        [TestCase( PolygonSubType.Rectangle, new int[] { 0, 0, 100, 0, 100, 50, 0, 50 } )]
        [TestCase( PolygonSubType.Rectangle, new int[] { 0, 0, 100, 0, 100, 52, -3, 50 } )]   // a bit disformed
        [TestCase( PolygonSubType.Square, new int[] { 0, 0, 100, 0, 100, 100, 0, 100 } )]
        [TestCase( PolygonSubType.Square, new int[] { 50, 0, 100, 50, 50, 100, 0, 50 } )]
        [TestCase( PolygonSubType.Square, new int[] { 51, 0, 100, 49, 50, 101, 1, 50 } )]    // a bit disformed
        [TestCase( PolygonSubType.Rhombus, new int[] { 30, 0, 60, 50, 30, 100, 0, 50 } )]
        [TestCase( PolygonSubType.Rhombus, new int[] { 0, 0, 100, 0, 130, 95, 30, 95 } )]
        [TestCase( PolygonSubType.Unknown, new int[] { 0, 0, 100, 0, 90, 50, 40, 70, 10, 40 } )]     // unknown if 5 corners or more
        public void CheckPolygonSubTypeTest( PolygonSubType expectedSubType, int[] corners )
        {
            ClassicAssert.AreEqual( expectedSubType, shapeChecker.CheckPolygonSubType( GetListOfPointFromArray( corners ) ) );
        }

        private List<IntPoint> GetListOfPointFromArray( int[] points )
        {
            List<IntPoint> list = new List<IntPoint>( );

            for ( int i = 0, n = points.Length; i < n; i += 2 )
            {
                list.Add( new IntPoint( points[i], points[i + 1] ) );
            }

            return list;
        }
    }
}

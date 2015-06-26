// Copyright (c) 2012 John McDonald and Gary Texmo
//
// This software is provided 'as-is', without any express or implied
// warranty. In no event will the authors be held liable for any damages
// arising from the use of this software.
//
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would
//    be appreciated but is not required.
//
// 2. Altered source versions must be plainly marked as such, and must not
//    be misrepresented as being the original software.
//
// 3. This notice may not be removed or altered from any source
//    distribution.
//
// DESCRIPTION:
//      Functions for drawing 2D primitives using XNA or MonoGame.


using System;
using System.Collections.Generic;
using MapGenerator.Properties;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MapGenerator.Libraries
{
	internal sealed class Primitives2D : IDisposable
	{
		private static readonly Dictionary<String, List<Vector2>> _circleCache = new Dictionary<string, List<Vector2>>();
		private readonly Texture2D _pixel;
	    private readonly SpriteBatch _spriteBatch;

        public Primitives2D(SpriteBatch spriteBatch)
		{
            if (spriteBatch == null)
            {
                throw(new ArgumentNullException("spriteBatch"));
            }

            _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixel.SetData(new[] { Color.White });
            _spriteBatch = spriteBatch;
		}

		/// <summary>Creates a list of vectors that represents a circle.</summary>
		/// <param name="radius">The radius of the circle.</param>
		/// <param name="sides">The number of sides to generate.</param>
        /// <returns>A list of vectors that, if connected, will create a circle.</returns>
		static private List<Vector2> CreateCircle(double radius, int sides)
		{
			// Look for a cached version of this circle.
			String circleKey = radius + "x" + sides;
			if (_circleCache.ContainsKey(circleKey))
			{
				return _circleCache[circleKey];
			}

			List<Vector2> vectors = new List<Vector2>();

			const double max = 2.0 * Math.PI;
			double step = max / sides;

			for (double theta = 0.0; theta < max; theta += step)
			{
				vectors.Add(new Vector2((float)(radius * Math.Cos(theta)), (float)(radius * Math.Sin(theta))));
			}

			// Then add the first vector again so it's a complete loop.
			vectors.Add(new Vector2((float)(radius * Math.Cos(0)), (float)(radius * Math.Sin(0))));

			// Cache this circle so that it can be quickly drawn next time.
			_circleCache.Add(circleKey, vectors);

			return vectors;
		}

		/// <summary>Creates a list of vectors that represents an arc.</summary>
		/// <param name="radius">The radius of the arc.</param>
		/// <param name="sides">The number of sides to generate in the circle that this will cut out from.</param>
		/// <param name="startingAngle">The starting angle of arc, 0 being to the east, increasing as you go clockwise.</param>
		/// <param name="radians">The radians to draw, clockwise from the starting angle.</param>
        /// <returns>A list of vectors that, if connected, will create an arc.</returns>
		static private List<Vector2> CreateArc(float radius, int sides, float startingAngle, float radians)
		{
			List<Vector2> points = new List<Vector2>();
			points.AddRange(CreateCircle(radius, sides));
			points.RemoveAt(points.Count - 1); // remove the last point because it's a duplicate of the first

			// The circle starts at (radius, 0).
			double curAngle = 0.0;
			double anglePerSide = MathHelper.TwoPi / sides;

			// "Rotate" to the starting point.
			while ((curAngle + (anglePerSide / 2.0)) < startingAngle)
			{
				curAngle += anglePerSide;

				// Move the first point to the end.
				points.Add(points[0]);
				points.RemoveAt(0);
			}

			// Add the first point, just in case we make a full circle.
			points.Add(points[0]);

			// Now remove the points at the end of the circle to create the arc.
			int sidesInArc = (int)((radians / anglePerSide) + 0.5);
			points.RemoveRange(sidesInArc + 1, points.Count - sidesInArc - 1);

			return points;
		}

        /// <summary>Draws a list of connecting points.</summary>
        /// <param name="position">Where to position the points.</param>
        /// <param name="points">The points to connect with lines.</param>
        /// <param name="color">The color to use.</param>
        /// <param name="thickness">The thickness of the lines.</param>
        private void DrawPoints(Vector2 position, List<Vector2> points, Color color, float thickness)
        {
            if (points.Count < 2)
            {
                return;
            }

            for (int i = 1; i < points.Count; i++)
            {
                DrawLine(points[i - 1] + position, points[i] + position, color, thickness);
            }
        }

	    /// <summary>Draws a line from point1 to point2 with an offset.</summary>
		/// <param name="point1">The first point.</param>
		/// <param name="point2">The second point.</param>
		/// <param name="color">The color to use.</param>
        /// <param name="thickness">The thickness of the line.</param>
        [PublicAPI]
		public void DrawLine(Vector2 point1, Vector2 point2, Color color, float thickness)
        {
			// Calculate the distance between the two vectors.
			float distance = Vector2.Distance(point1, point2);

			// Calculate the angle between the two vectors.
			float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);

            // Stretch the pixel between the two vectors.
	        _spriteBatch.Draw(_pixel, point1, null, color, angle, Vector2.Zero, new Vector2(distance, thickness),
	            SpriteEffects.None, 0);
        }

		/// <summary>Draw a circle.</summary>
		/// <param name="center">The center of the circle.</param>
		/// <param name="radius">The radius of the circle.</param>
		/// <param name="sides">The number of sides to generate.</param>
		/// <param name="color">The color of the circle.</param>
		/// <param name="thickness">The thickness of the lines used.</param>
        [PublicAPI]
		public void DrawCircle(Vector2 center, float radius, int sides, Color color, float thickness)
		{
			DrawPoints(center, CreateCircle(radius, sides), color, thickness);
		}

	    /// <summary>Draw an arc.</summary>
		/// <param name="center">The center of the arc.</param>
		/// <param name="radius">The radius of the arc.</param>
		/// <param name="sides">The number of sides to generate.</param>
		/// <param name="startingAngle">The starting angle of arc, 0 being to the east, increasing as you go clockwise.</param>
		/// <param name="radians">The number of radians to draw, clockwise from the starting angle.</param>
		/// <param name="color">The color of the arc.</param>
        /// <param name="thickness">The thickness of the arc.</param>
        [PublicAPI]
		public void DrawArc(Vector2 center, float radius, int sides, float startingAngle, float radians, Color color, float thickness)
		{
			List<Vector2> arc = CreateArc(radius, sides, startingAngle, radians);
			DrawPoints(center, arc, color, thickness);
		}

	    public void Dispose()
	    {
	        if (_pixel != null && !_pixel.IsDisposed)
	        {
	            _pixel.Dispose();
	        }
	    }
	}
}

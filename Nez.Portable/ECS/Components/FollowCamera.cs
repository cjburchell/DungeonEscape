using Microsoft.Xna.Framework;


// ReSharper disable once CheckNamespace
namespace Nez
{
	/// <summary>
	/// basic follow camera. LockOn mode uses no deadzone and just centers the camera on the target. CameraWindow mode wraps a deadzone
	/// around the target allowing it to move within the deadzone without moving the camera.
	/// </summary>
	public class FollowCamera : Component, IUpdatable
	{
		public enum CameraStyle
		{
			LockOn,
			CameraWindow
		}

		public enum Measurement
		{
			/// <summary>
			/// Size is measured in pixel.
			/// Does not change with the Camera <see cref="Camera.Bounds"/> and <see cref="Camera.Zoom"/> level.
			/// </summary>
			FixedPixel,

			/// <summary>
			/// Size is measured in % of Camera <see cref="Camera.Bounds"/>.
			/// Where 1.0f equals the whole Camera Size and 0.5f is half the Camera size.
			/// Scales automatically with the Camera <see cref="Camera.Bounds"/> and <see cref="Camera.Zoom"/> level.
			/// </summary>
			ScaledCameraBounds
		}

		public Camera Camera;

		/// <summary>
		/// how fast the camera closes the distance to the target position
		/// </summary>
		public float FollowLerp = 0.1f;

		/// <summary>
		/// when in <see cref="CameraStyle.CameraWindow"/> mode used as a bounding box around the camera position
		/// to allow the targetEntity to movement inside it without moving the camera.
		/// when in <see cref="CameraStyle.LockOn"/> mode only the deadzone x/y values are used as offset.
		/// This is set to sensible defaults when you call <see cref="Follow"/> but you are
		/// free to override <see cref="Deadzone"/> to get a custom deadzone directly or via the helper <see cref="SetCenteredDeadZone"/>.
		/// </summary>
		public RectangleF Deadzone;

		/// <summary>
		/// offset from the screen center that the camera will focus on
		/// </summary>
		public Vector2 FocusOffset;

		/// <summary>
		/// If true, the camera position will not got out of the map rectangle (0,0, mapwidth, mapheight)
		/// </summary>
		public bool MapLockEnabled;

		/// <summary>
		/// Contains the width and height of the current map.
		/// </summary>
		public Vector2 MapSize;

		Entity _targetEntity;
		Collider _targetCollider;
		Vector2 _desiredPositionDelta;
		CameraStyle _cameraStyle;
		Measurement _deadZoneMeasurement;
		RectangleF _worldSpaceDeadZone;


		public FollowCamera(Entity targetEntity, Camera camera, CameraStyle cameraStyle = CameraStyle.LockOn,
			Measurement deadZoneMeasurement = Measurement.FixedPixel)
		{
			_targetEntity = targetEntity;
			_cameraStyle = cameraStyle;
			_deadZoneMeasurement = deadZoneMeasurement;
			Camera = camera;
		}

		public FollowCamera(Entity targetEntity, CameraStyle cameraStyle = CameraStyle.LockOn,
			Measurement deadZoneMeasurement = Measurement.FixedPixel)
			: this(targetEntity, null, cameraStyle, deadZoneMeasurement)
		{
		}

		public FollowCamera() : this(null, null)
		{
		}

		public override void OnAddedToEntity()
		{
			if (Camera == null)
				Camera = Entity.Scene.Camera;

			Follow(_targetEntity, _cameraStyle);

			// listen for changes in screen size so we can keep our deadzone properly positioned
			Core.Emitter.AddObserver(CoreEvents.GraphicsDeviceReset, OnGraphicsDeviceReset);
		}

		public override void OnRemovedFromEntity()
		{
			Core.Emitter.RemoveObserver(CoreEvents.GraphicsDeviceReset, OnGraphicsDeviceReset);
		}

		public virtual void Update()
		{
			// calculate the current deadzone around the camera
			// Camera.Position is the center of the camera view
			if (_deadZoneMeasurement == Measurement.FixedPixel)
			{
				//Calculate in Pixel Units
				_worldSpaceDeadZone.X = Camera.Position.X + Deadzone.X + FocusOffset.X;
				_worldSpaceDeadZone.Y = Camera.Position.Y + Deadzone.Y + FocusOffset.Y;
				_worldSpaceDeadZone.Width = Deadzone.Width;
				_worldSpaceDeadZone.Height = Deadzone.Height;
			}
			else
			{
				//Scale everything according to Camera Bounds
				var screenSize = Camera.Bounds.Size;
				_worldSpaceDeadZone.X = Camera.Position.X + (screenSize.X * Deadzone.X) + FocusOffset.X;
				_worldSpaceDeadZone.Y = Camera.Position.Y + (screenSize.Y * Deadzone.Y) + FocusOffset.Y;
				_worldSpaceDeadZone.Width = screenSize.X * Deadzone.Width;
				_worldSpaceDeadZone.Height = screenSize.Y * Deadzone.Height;
			}

			if (_targetEntity != null)
				UpdateFollow();

			Camera.Position = Vector2.Lerp(Camera.Position, Camera.Position + _desiredPositionDelta, FollowLerp);
			Camera.Entity.Transform.RoundPosition();

			if (MapLockEnabled)
			{
				Camera.Position = ClampToMapSize(Camera.Position);
				Camera.Entity.Transform.RoundPosition();
			}
		}

		/// <summary>
		/// Clamps the camera so it never leaves the visible area of the map.
		/// </summary>
		/// <returns>The to map size.</returns>
		/// <param name="position">Position.</param>
		Vector2 ClampToMapSize(Vector2 position)
		{
			var halfScreen = new Vector2(Camera.Bounds.Width, Camera.Bounds.Height) * 0.5f;
			var cameraMax = new Vector2(MapSize.X - halfScreen.X, MapSize.Y - halfScreen.Y);

			return Vector2.Clamp(position, halfScreen, cameraMax);
		}

		public override void DebugRender(Batcher batcher)
		{
			if (_cameraStyle == CameraStyle.LockOn)
				batcher.DrawHollowRect(_worldSpaceDeadZone.X - 5, _worldSpaceDeadZone.Y - 5, 10, 10, Color.DarkRed);
			else
				batcher.DrawHollowRect(_worldSpaceDeadZone, Color.DarkRed);
		}

		void OnGraphicsDeviceReset()
		{
			// we need this to occur on the next frame so the camera bounds are updated
			Core.Schedule(0f, this, t =>
			{
				var self = t.Context as FollowCamera;
				self?.Follow(self._targetEntity, self._cameraStyle, self._deadZoneMeasurement);
			});
		}

		void UpdateFollow()
		{
			_desiredPositionDelta.X = _desiredPositionDelta.Y = 0;

			if (_cameraStyle == CameraStyle.LockOn)
			{
				var targetX = _targetEntity.Transform.Position.X;
				var targetY = _targetEntity.Transform.Position.Y;

				// x-axis
				if (_worldSpaceDeadZone.X > targetX || _worldSpaceDeadZone.X < targetX)
					_desiredPositionDelta.X = targetX - _worldSpaceDeadZone.X;

				// y-axis
				if (_worldSpaceDeadZone.Y < targetY || _worldSpaceDeadZone.Y > targetY)
					_desiredPositionDelta.Y = targetY - _worldSpaceDeadZone.Y;
			}
			else
			{
				// make sure we have a targetCollider for CameraWindow. If we dont bail out.
				if (_targetCollider == null)
				{
					_targetCollider = _targetEntity.GetComponent<Collider>();
					if (_targetCollider == null)
						return;
				}

				var targetBounds = _targetEntity.GetComponent<Collider>().Bounds;
				if (!_worldSpaceDeadZone.Contains(targetBounds))
				{
					// x-axis
					if (_worldSpaceDeadZone.Left > targetBounds.Left)
						_desiredPositionDelta.X = targetBounds.Left - _worldSpaceDeadZone.Left;
					else if (_worldSpaceDeadZone.Right < targetBounds.Right)
						_desiredPositionDelta.X = targetBounds.Right - _worldSpaceDeadZone.Right;

					// y-axis
					if (_worldSpaceDeadZone.Bottom < targetBounds.Bottom)
						_desiredPositionDelta.Y = targetBounds.Bottom - _worldSpaceDeadZone.Bottom;
					else if (_worldSpaceDeadZone.Top > targetBounds.Top)
						_desiredPositionDelta.Y = targetBounds.Top - _worldSpaceDeadZone.Top;
				}
			}
		}

		public void Follow(Entity targetEntity, CameraStyle cameraStyle = CameraStyle.CameraWindow,
			Measurement deadZoneMeasurement = Measurement.ScaledCameraBounds)
		{
			_targetEntity = targetEntity;
			_cameraStyle = cameraStyle;

			var cameraBounds = Camera.Bounds;

			// Set a default deadzone to match common usecases
			switch (_cameraStyle)
			{
				case CameraStyle.CameraWindow:
					if (deadZoneMeasurement == Measurement.ScaledCameraBounds)
						SetCenteredDeadzoneInScreenspace(1.0f / 6.0f, 1.0f / 3.0f);
					else
						SetCenteredDeadZone((int)cameraBounds.Width / 6, (int)cameraBounds.Height / 3);
					break;
				case CameraStyle.LockOn:
					if (deadZoneMeasurement == Measurement.ScaledCameraBounds)
						SetCenteredDeadzoneInScreenspace(0, 0);
					else
						SetCenteredDeadZone(10, 10);
					break;
			}
		}

		/// <summary>
		/// sets up the deadzone centered in the current cameras bounds with the given size
		/// </summary>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public void SetCenteredDeadZone(int width, int height)
		{
			Deadzone = new RectangleF(-width / 2.0f, -height / 2.0f, width, height);
			_deadZoneMeasurement = Measurement.FixedPixel;
		}

		/// <summary>
		/// sets up the deadzone centered in the current cameras bounds with the given size
		/// </summary>
		/// <param name="width">Width in % of screenspace. Between 0.0 and 1.0</param>
		/// <param name="height">Height in % of screenspace. Between 0.0 and 1.0</param>
		public void SetCenteredDeadzoneInScreenspace(float width, float height)
		{
			Deadzone = new RectangleF(-width / 2, -height / 2, width, height);
			_deadZoneMeasurement = Measurement.ScaledCameraBounds;
		}
	}
}
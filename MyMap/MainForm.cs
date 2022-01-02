using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsForms.ToolTips;
using MyMap.Model;

namespace MyMap
{
	public partial class MainForm : Form
	{
        public GMapMarker SelectedMarker { get; private set; }
		public GMapOverlay MainOverlay { get; private set; } = new GMapOverlay();
		public bool IsLeftButtonDown { get; private set; } = false;

		public readonly SqlConnection SqlConnection = new SqlConnection("Data Source=localhost\\SQLEXPRESS;Initial Catalog=Map;Integrated Security=True");

		public MainForm()
		{
			InitializeComponent();
		}

        private void MainForm_Load(object sender, EventArgs e)
        {
			LoadGMap();
			LoadPointsFromDb();
		}

        private void Gmap_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				IsLeftButtonDown = true;

			//находим тот маркер над которым нажали клавишу мыши
			SelectedMarker = gmap.Overlays
                .SelectMany(o => o.Markers)
                .FirstOrDefault(m => m.IsMouseOver == true);
        }

		private void Gmap_MouseUp(object sender, MouseEventArgs e)
		{
			if (SelectedMarker is null)
                return;

			if (e.Button == MouseButtons.Left)
			{
				//переводим координаты курсора мыши в долготу и широту на карте
				var latlng = gmap.FromLocalToLatLng(e.X, e.Y);
				//присваиваем новую позицию для маркера
				SelectedMarker.Position = latlng;
				SelectedMarker.ToolTipText = $"{latlng.Lat}; {latlng.Lng}";
				SelectedMarker = null;
			}			
        }

		private void LoadGMap()
		{
			// Настройки для компонента GMap
			gmap.Bearing = 0;
			// Перетаскивание правой кнопки мыши
			gmap.CanDragMap = true;
			// Перетаскивание карты левой кнопкой мыши
			gmap.DragButton = MouseButtons.Left;

			gmap.GrayScaleMode = true;

			// Все маркеры будут показаны
			gmap.MarkersEnabled = true;

			gmap.MaxZoom = 18;
			gmap.MinZoom = 2;
			gmap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;

			// Отключение негативного режима
			gmap.NegativeMode = false;
			gmap.PolygonsEnabled = true;
			gmap.RoutesEnabled = true;
			gmap.ShowTileGridLines = false;
			gmap.Zoom = 10;

			// Чья карта используется
			gmap.MapProvider = GMapProviders.GoogleMap;

			// Загрузка этой точки на карте
			GMaps.Instance.Mode = AccessMode.ServerOnly;
			gmap.Position = new PointLatLng(55.0415, 82.9346);
			MainOverlay.IsVisibile = true;
			gmap.Overlays.Add(MainOverlay);

			gmap.MouseUp += Gmap_MouseUp;
			gmap.MouseDown += Gmap_MouseDown;
			gmap.MouseMove += new MouseEventHandler(GmapMouseMove);
		}

		/// <summary>
		/// Метод, отвечающий за перемещение маркера ЛКМ по карте и отображения подсказки с текущими координатами маркера
		/// </summary>
		private void GmapMouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (SelectedMarker != null)
				{
					PointLatLng point = gmap.FromLocalToLatLng(e.X, e.Y);
					SelectedMarker.Position = point;
					SelectedMarker.ToolTipText = $"{point.Lat}; {point.Lng}";
				}
			}
		}

		private void LoadPointsFromDb()
		{
			SqlConnection.Open();

			var sqlCommand = new SqlCommand("SELECT * FROM Points", SqlConnection);
			var sqlDataReader = sqlCommand.ExecuteReader();
			var points = new List<MapPoint>();

			if (sqlDataReader.HasRows)
			{
				while (sqlDataReader.Read())
					points.Add(new MapPoint(Convert.ToDouble(sqlDataReader[1]), Convert.ToDouble(sqlDataReader[2])));
			}

			sqlDataReader.Close();

			for (int i = 0; i < points.Count; i++)
			{
				var gMarkerGoogle = new GMarkerGoogle(new PointLatLng(points[i].X, points[i].Y), GMarkerGoogleType.lightblue);
				gMarkerGoogle.ToolTip = new GMapRoundedToolTip(gMarkerGoogle);
				gMarkerGoogle.ToolTipText = $"{points[i].X}; {points[i].Y}";
				MainOverlay.Markers.Add(gMarkerGoogle);
			}
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			MainOverlay.Markers.Clear();
		}

		private void gmap_MouseDoubleClick(object sender, MouseEventArgs e)
		{
            if (e.Button == MouseButtons.Left)
            {
                // Широта - latitude - lat - с севера на юг
                double x = gmap.FromLocalToLatLng(e.X, e.Y).Lat;
                // Долгота - longitude - lng - с запада на восток
                double y = gmap.FromLocalToLatLng(e.X, e.Y).Lng;

                // Добавляем метку на слой
                var markerWithMyPosition = new GMarkerGoogle(new PointLatLng(x, y), GMarkerGoogleType.lightblue);
                markerWithMyPosition.ToolTip = new GMapRoundedToolTip(markerWithMyPosition);
                markerWithMyPosition.ToolTipText = $"{x}; {y}";
                MainOverlay.Markers.Add(markerWithMyPosition);
            }
        }

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			var markers = MainOverlay.Markers;

			var sqlCommand = new SqlCommand("DELETE FROM Points", SqlConnection);
			sqlCommand.ExecuteNonQuery();

			var stringBuilder = new StringBuilder("INSERT INTO [Points] (X, Y) VALUES ");
			var comma = ",";
			for (int i = 0; i < markers.Count; i++)
			{
				if (i + 1 == markers.Count)
					comma = "";

				stringBuilder.Append($"('{markers[i].Position.Lat}', '{markers[i].Position.Lng}'){comma} ");				
			}

			if (markers.Count > 0)
			{
				sqlCommand.CommandText = stringBuilder.ToString();
				sqlCommand.ExecuteNonQuery();
			}			
			SqlConnection.Close();
		}
	}
}

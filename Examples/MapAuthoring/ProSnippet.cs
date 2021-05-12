/*

   Copyright 2018 Esri

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

   See the License for the specific language governing permissions and
   limitations under the License.

*/
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.DeviceLocation;
using ArcGIS.Desktop.Core.DeviceLocation.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.DeviceLocation;
using ArcGIS.Desktop.Mapping.Offline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapAuthoring.ProSnippet
{
  class ProSnippet
  {
    #region ProSnippet Group: Maps
    #endregion
    public void GetActiveMapAsync()
    {
      #region Get the active map

      Map map = MapView.Active.Map;
      #endregion
    }


    public async Task CreateMapAsync(string mapName)
    {

      #region Create a new map with a default basemap layer

      await QueuedTask.Run(() =>
      {
        var map = MapFactory.Instance.CreateMap(mapName, basemap: Basemap.ProjectDefault);
        //TODO: use the map...
      });

      #endregion
    }

    #region Find a map within a project and open it
    public static async Task<Map> FindOpenExistingMapAsync(string mapName)
    {
      return await QueuedTask.Run(async () =>
      {
        Map map = null;
        Project proj = Project.Current;

        //Finding the first project item with name matches with mapName
        MapProjectItem mpi =
            proj.GetItems<MapProjectItem>()
                .FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.CurrentCultureIgnoreCase));
        if (mpi != null)
        {
          map = mpi.GetMap();
          //Opening the map in a mapview
          await ProApp.Panes.CreateMapPaneAsync(map);
        }
        return map;
      });

    }
    #endregion


    public async Task<Map> OpenWebMapAsync()
    {

      #region Open a webmap
      Map map = null;

      //Assume we get the selected webmap from the Project pane's Portal tab
      if (Project.Current.SelectedItems.Count > 0)
      {
        if (MapFactory.Instance.CanCreateMapFrom(Project.Current.SelectedItems[0]))
        {
          map = MapFactory.Instance.CreateMapFromItem(Project.Current.SelectedItems[0]);
          await ProApp.Panes.CreateMapPaneAsync(map);
        }
      }

      #endregion

      return map;
    }

    #region Get Map Panes
    public static IEnumerable<IMapPane> GetMapPanes()
    {
      //Sorted by Map Uri
      return ProApp.Panes.OfType<IMapPane>().OrderBy((mp) => mp.MapView.Map.URI ?? mp.MapView.Map.Name);
    }
    #endregion

    #region Get the Unique List of Maps From the Map Panes
    public static IReadOnlyList<Map> GetMapsFromMapPanes()
    {
      //Gets the unique list of Maps from all the MapPanes.
      //Note: The list of maps retrieved from the MapPanes
      //maybe less than the total number of Maps in the project.
      //It depends on what maps the user has actually opened.
      var mapPanes = ProApp.Panes.OfType<IMapPane>()
                  .GroupBy((mp) => mp.MapView.Map.URI).Select(grp => grp.FirstOrDefault());
      List<Map> uniqueMaps = new List<Map>();
      foreach (var pane in mapPanes)
        uniqueMaps.Add(pane.MapView.Map);
      return uniqueMaps;
    }
    #endregion

    public void ModifyMapAndPane()
    {
      QueuedTask.Run(() =>
      {
        #region Change the Map name
        ////Note: call within QueuedTask.Run()
        MapView.Active.Map.SetName("Test");
        #endregion
      });

      #region Renames the caption of the pane
      ProApp.Panes.ActivePane.Caption = "Caption";
      #endregion

    }

    private Task ConvertMapToScene(Map map)
    {
      if (map == null) return Task.FromResult(0);
      return QueuedTask.Run(() =>
      {
        #region Convert Map to Local Scene
        //Note: Run within the context of QueuedTask.Run
        bool canConvertMap = MapFactory.Instance.CanConvertMap(map, MapConversionType.SceneLocal);
        if (canConvertMap)
          MapFactory.Instance.ConvertMap(map, MapConversionType.SceneLocal, true);
        #endregion
      });
    }

    private async void GetBasemaps()
    {
      #region Get Basemaps

      //Basemaps stored locally in the project. This is usually an empty collection
      string localBasemapTypeID = "cim_map_basemap";
      var localBasemaps = await QueuedTask.Run(() =>
      {
        var mapContainer = Project.Current.GetProjectItemContainer("Map");
        return mapContainer.GetItems().Where(i => i.TypeID == localBasemapTypeID).ToList();
      });

      //portal basemaps. If there is no current active portal, the usual default
      //is arcgis online
      var portal = ArcGISPortalManager.Current.GetActivePortal();
      var portalBaseMaps = await portal.GetBasemapsAsync();

      //use one of them...local or portal...
      //var map = MapView.Active.Map;
      //QueuedTask.Run(() => map?.SetBasemapLayers(portalBaseMaps[0]));

      #endregion
    }

    private void SaveMap()
    {
      Map map = null;

      #region Save Map as MapX

      map.SaveAsFile(@"C:\Data\MyMap.mapx");

      #endregion

    }

    private void ClipMap()
    {
      QueuedTask.Run(() =>
      {
        #region Clip Map to the provided clip polygon
        //Run within QueuedTask
        var map = MapView.Active.Map;
        //A layer to use for the clip extent
        var lyrOfInterest = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(l => l.Name == "TestPoly").FirstOrDefault();
        //Get the polygon to use to clip the map
        var extent = lyrOfInterest.QueryExtent();
        var polygonForClipping = PolygonBuilder.CreatePolygon(extent);
        //Clip the map using the layer's extent
        map.SetClipGeometry(polygonForClipping,
            SymbolFactory.Instance.ConstructLineSymbol(
              SymbolFactory.Instance.ConstructStroke(
                ColorFactory.Instance.BlueRGB, 2.0, SimpleLineStyle.Dash)));
        #endregion
      });
    }

    private void ClearClipMap()
    {
      QueuedTask.Run(() =>
      {
        #region Clear the current map clip geometry 
        //Run within QueuedTask
        var map = MapView.Active.Map;
        //Clear the Map clip.
        //If no clipping is set then this is a no-op.
        map.ClearClipGeometry();
        #endregion
      });
    }

    private void GetClipMapGeometry()
    {
      QueuedTask.Run(() =>
      {
        #region Get the map clipping geometry
        var map = MapView.Active.Map;
        //If clipping is set to ArcGIS.Core.CIM.ClippingMode.None or ArcGIS.Core.CIM.ClippingMode.MapSeries null is returned
        //If clipping is set to ArcGIS.Core.CIM.ClippingMode.MapExtent the ArcGIS.Core.CIM.CIMMap.CustomFullExtent is returned.
        //Otherwise, if clipping is set to ArcGIS.Core.CIM.ClippingMode.CustomShape the custom clip polygon is returned.
        var poly = map.GetClipGeometry();
        //You can use the polygon returned
        //For example: We make a polygon graphic element and add it to a Graphics Layer.
        var gl = map.GetLayersAsFlattenedList().OfType<GraphicsLayer>().FirstOrDefault();
        if (gl == null) return;
        var polygonSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(CIMColor.CreateRGBColor(255, 255, 0));
        var cimGraphicElement = new CIMPolygonGraphic
        {
          Polygon = poly,
          Symbol = polygonSymbol.MakeSymbolReference()
        };
        gl.AddElement(cimGraphicElement);
        #endregion
      });
    }

    public void LocationUnit1()
		{
      Map map = null;

      #region Get the Current Map Location Unit

      //var map = MapView.Active.Map;
      //Must be on the QueuedTask.Run()

      //Get the current location unit
      var loc_unit = map.GetLocationUnitFormat();
      var line = $"{loc_unit.DisplayName}, {loc_unit.UnitCode}";
      System.Diagnostics.Debug.WriteLine(line);

      #endregion
    }

    public void LocationUnit1b()
    {
      Map map = null;

      #region Get the Available List of Map Location Units

      //var map = MapView.Active.Map;
      //Must be on the QueuedTask.Run()

      //Linear location unit formats are not included if the map sr
      //is geographic.
      var loc_units = map.GetAvailableLocationUnitFormats();

      #endregion
    }

    public void LocationUnit2()
    {
      #region Format a Location Using the Current Map Location Unit

      var mv = MapView.Active;
      var map = mv.Map;

      QueuedTask.Run(() =>
      {
        //Get the current view camera location
        var center_pt = new Coordinate2D(mv.Camera.X, mv.Camera.Y);
        //Get the current location unit
        var loc_unit = map.GetLocationUnitFormat();

        //Format the camera location
        var str = loc_unit.FormatLocation(center_pt, map.SpatialReference);
        System.Diagnostics.Debug.WriteLine($"Formatted location: {str}");
      });

      #endregion
    }

    public void LocationUnit3()
    {
      #region Set the Location Unit for the Current Map

      var mv = MapView.Active;
      var map = mv.Map;

      QueuedTask.Run(() =>
      {
        //Get the list of available location unit formats
        //for the current map
        var loc_units = map.GetAvailableLocationUnitFormats();

        //arbitrarily use the last unit in the list
        map.SetLocationUnitFormat(loc_units.Last());
      });

      #endregion
    }

    public void ElevationUnit1()
    {
      Map map = null;

      #region Get the Current Map Elevation Unit

      //var map = MapView.Active.Map;
      //Must be on the QueuedTask.Run()

      //If the map is not a scene, the default Project distance
      //unit will be returned
      var elev_unit = map.GetElevationUnitFormat();
      var line = $"{elev_unit.DisplayName}, {elev_unit.UnitCode}";
      System.Diagnostics.Debug.WriteLine(line);

      #endregion
    }

    public void ElevationUnit1b()
    {
      Map map = null;

      #region Get the Available List of Map Elevation Units

      //var map = MapView.Active.Map;
      //Must be on the QueuedTask.Run()

      //If the map is not a scene, the list of current
      //Project distance units will be returned
      var elev_units = map.GetAvailableElevationUnitFormats();

      #endregion
    }

    public void ElevationUnit2()
    {
      #region Format an Elevation Using the Current Map Elevation Unit

      var mv = MapView.Active;
      var map = mv.Map;

      QueuedTask.Run(() =>
      {
        //Get the current elevation unit. If the map is not
        //a scene the default Project distance unit is returned
        var elev_unit = map.GetElevationUnitFormat();

        //Format the view camera elevation
        var str = elev_unit.FormatValue(mv.Camera.Z);

        System.Diagnostics.Debug.WriteLine($"Formatted elevation: {str}");
      });

      #endregion
    }

    public void ElevationUnit3()
    {
      #region Set the Elevation Unit for the Current Map

      var map = MapView.Active.Map;

      QueuedTask.Run(() =>
      {
        //Trying to set the elevation unit on a map other than
        //a scene will throw an InvalidOperationException
				if (map.IsScene)
				{
					//Get the list of available elevation unit formats
					//for the current map
					var loc_units = map.GetAvailableElevationUnitFormats();
          //arbitrarily use the last unit in the list
          map.SetElevationUnitFormat(loc_units.Last());
        }
				
      });

      #endregion
    }

		#region ProSnippet Group: Offline Map
		#endregion

    public void OfflineMaps1()
		{
      #region Check Map Has Sync-Enabled Content

      //namespace ArcGIS.Desktop.Mapping.Offline
      var map = MapView.Active.Map;

      //await if needed...
      QueuedTask.Run(() =>
      {
        var hasSyncEnabledContent = GenerateOfflineMap.Instance.GetCanGenerateReplicas(map);
        if (hasSyncEnabledContent)
				{
          //TODO - use status...
				}
      });
      #endregion
    }

    public void OfflineMaps2()
    {
      #region Generate Replicas for Sync-Enabled Content

      //namespace ArcGIS.Desktop.Mapping.Offline
      var extent = MapView.Active.Extent;
      var map = MapView.Active.Map;

      //await if needed...
      QueuedTask.Run(() =>
      {
        //Check map has sync-enabled content that can be taken offline
        var hasSyncEnabledContent = GenerateOfflineMap.Instance.GetCanGenerateReplicas(map);
        if (hasSyncEnabledContent)
        {
          //Generate Replicas and take the content offline
          //sync-enabled content gets copied local into a SQLite DB
          var gen_params = new GenerateReplicaParams()
          {
            Extent = extent, //SR of extent must match map SR

            //DestinationFolder can be left blank, if specified,
            //it must exist. Defaults to project offline maps location
            DestinationFolder = @"C:\Data\Offline"
          };
          //Sync-enabled layer content will be resourced to point to the
          //local replica content.
          GenerateOfflineMap.Instance.GenerateReplicas(map, gen_params);

        }
      });
      #endregion
    }

    public void OfflineMaps3()
    {
      #region Check Map Has Local Syncable Content

      //namespace ArcGIS.Desktop.Mapping.Offline
      var map = MapView.Active.Map;

      //await if needed...
      QueuedTask.Run(() =>
      {
        //Check map has local syncable content
        var canSyncContent = GenerateOfflineMap.Instance.GetCanSynchronizeReplicas(map);
        if (canSyncContent)
        {
          //TODO - use status
        }
      });
      #endregion
    }

    public void OfflineMaps4()
    {
      #region Synchronize Replicas for Syncable Content

      //namespace ArcGIS.Desktop.Mapping.Offline
      var map = MapView.Active.Map;

      //await if needed...
      QueuedTask.Run(() =>
      {
        //Check map has local syncable content
        var canSyncContent = GenerateOfflineMap.Instance.GetCanSynchronizeReplicas(map);
        if (canSyncContent)
        {
          //Sync Replicas - changes since last sync are pushed to the
          //parent replica. Parent changes are pulled to the client.
          //Unsaved edits are _not_ sync'd. 
          GenerateOfflineMap.Instance.SynchronizeReplicas(map);
        }
      });
      #endregion
    }

    public void OfflineMaps5()
    {
      #region Remove Replicas for Syncable Content

      //namespace ArcGIS.Desktop.Mapping.Offline
      var extent = MapView.Active.Extent;
      var map = MapView.Active.Map;

      //await if needed...
      QueuedTask.Run(() =>
      {
        //Check map has local syncable content
        //Either..
        //var canSyncContent = GenerateOfflineMap.Instance.GetCanSynchronizeReplicas(map);
        //Or...both accomplish the same thing...
        var canRemove = GenerateOfflineMap.Instance.GetCanRemoveReplicas(map);
        if (canRemove)
        {
          //Remove Replicas - any unsync'd changes are lost
          //Call sync _first_ to push any outstanding changes if
          //needed. Local syncable content is re-sourced
          //to point to the service
          GenerateOfflineMap.Instance.RemoveReplicas(map);
        }
      });
      #endregion
    }

    public void OfflineMaps6()
    {
      #region Export Map Raster Tile Cache Content

      //namespace ArcGIS.Desktop.Mapping.Offline
      var extent = MapView.Active.Extent;
      var map = MapView.Active.Map;

      //await if needed...
      QueuedTask.Run(() =>
      {
        //Does the map have any exportable raster content?
        var canExport = GenerateOfflineMap.Instance.GetCanExportRasterTileCache(map);
        if (canExport)
        {
          //Check the available LOD scale ranges
          var scales = GenerateOfflineMap.Instance.GetExportRasterTileCacheScales(map, extent);
          //Pick the desired LOD scale
          var max_scale = scales[scales.Count() / 2];

          //Configure the export parameters
          var export_params = new ExportTileCacheParams()
          {
            Extent = extent,//Use same extent as was used to retrieve scales
            MaximumUserDefinedScale = max_scale
            //DestinationFolder = .... (optional)
          };
          //If DestinationFolder is not set, output defaults to project
          //offline maps location set in the project properties. If that is 
          //not set, output defaults to the current project folder location.

          //Do the export. Depending on the MaximumUserDefinedScale and the
          //area of the extent requested, this can take minutes for tile packages
          //over 1 GB or less if your network speed is slow...
          GenerateOfflineMap.Instance.ExportRasterTileCache(map, export_params);
        }
      });
      #endregion
    }

    public void OfflineMaps7()
    {
      #region Export Map Vector Tile Cache Content

      //namespace ArcGIS.Desktop.Mapping.Offline
      var extent = MapView.Active.Extent;
      var map = MapView.Active.Map;

      //await if needed...
      QueuedTask.Run(() =>
      {
        //Does the map have any exportable vector tile content?
        var canExport = GenerateOfflineMap.Instance.GetCanExportVectorTileCache(map);
        if (canExport)
        {
          //Check the available LOD scale ranges
          var scales = GenerateOfflineMap.Instance.GetExportVectorTileCacheScales(map, extent);
          //Pick the desired LOD scale
          var max_scale = scales[scales.Count() / 2];

          //Configure the export parameters
          var export_params = new ExportTileCacheParams()
          {
            Extent = extent,//Use same extent as was used to retrieve scales
            MaximumUserDefinedScale = max_scale,
            DestinationFolder = @"C:\Data\Offline"
          };
          //If DestinationFolder is not set, output defaults to project
          //offline maps location set in the project properties. If that is 
          //not set, output defaults to the current project folder location.

          //Do the export. Depending on the MaximumUserDefinedScale and the
          //area of the extent requested, this can take minutes for tile packages
          //over 1 GB or less if your network speed is slow...
          GenerateOfflineMap.Instance.ExportVectorTileCache(map, export_params);
        }
      });
      #endregion
    }

    #region ProSnippet Group: Create Layer
    #endregion
    public async Task AddLayerAsync()
    {
      Map map = null;

      #region Create and add a layer to the active map

      /*
* string url = @"c:\data\project.gdb\DEM";  //Raster dataset from a FileGeodatabase
* string url = @"c:\connections\mySDEConnection.sde\roads";  //FeatureClass of a SDE
* string url = @"c:\connections\mySDEConnection.sde\States\roads";  //FeatureClass within a FeatureDataset from a SDE
* string url = @"c:\data\roads.shp";  //Shapefile
* string url = @"c:\data\imagery.tif";  //Image from a folder
* string url = @"c:\data\mySDEConnection.sde\roads";  //.lyrx or .lpkx file
* string url = @"c:\data\CAD\Charlottesville\N1W1.dwg\Polyline";  //FeatureClass in a CAD dwg file
* string url = @"C:\data\CAD\UrbanHouse.rvt\Architectural\Windows"; //Features in a Revit file
* string url = @"http://sampleserver1.arcgisonline.com/ArcGIS/rest/services/Demographics/ESRI_Census_USA/MapServer";  //map service
* string url = @"http://sampleserver6.arcgisonline.com/arcgis/rest/services/NapervilleShelters/FeatureServer/0";  //FeatureLayer off a map service or feature service
*/
      string url = @"c:\data\project.gdb\roads";  //FeatureClass of a FileGeodatabase

      Uri uri = new Uri(url);
      await QueuedTask.Run(() => LayerFactory.Instance.CreateLayer(uri, MapView.Active.Map));

      #endregion

      #region Create layer with create-params
      var flyrCreatnParam = new FeatureLayerCreationParams(new Uri(@"c:\data\world.gdb\cities"))
      {
        Name = "World Cities",
        IsVisible = false,
        MinimumScale = 1000000,
        MaximumScale = 5000,
        DefinitionFilter = new CIMDefinitionFilter()
        {
          DefinitionExpression = "Population > 100000",
          Name = "More than 100k"
        },
        RendererDefinition = new SimpleRendererDefinition()
        {
          SymbolTemplate = SymbolFactory.Instance.ConstructPointSymbol(
            CIMColor.CreateRGBColor(255, 0, 0), 8, SimpleMarkerStyle.Hexagon).MakeSymbolReference()
        }
      };

      var featureLayer = LayerFactory.Instance.CreateLayer<FeatureLayer>(
        flyrCreatnParam, map, LayerPosition.AutoArrange);
      #endregion

    }
    public static void CreateLayerWithParams()
    {

      #region Create FeatureLayer and add to Map using LayerCreationParams
      //Note: Call within QueuedTask.Run()
      var layerDoc = new LayerDocument(@"E:\Data\SDK\Default2DPointSymbols.lyrx");
      var createParams = new LayerCreationParams(layerDoc.GetCIMLayerDocument());
      LayerFactory.Instance.CreateLayer<FeatureLayer>(createParams, MapView.Active.Map);
      #endregion
    }
    public static FeatureLayer CreateLayerWithOptions()
    {
      #region Create FeatureLayer and set to not display in Map.
      //The catalog path of the feature layer to add to the map
      var featureClassUriVisibility = new Uri(@"C:\Data\Admin\AdminData.gdb\USA\cities");
      //Define the Feature Layer's parameters.
      var layerParamsVisibility = new FeatureLayerCreationParams(featureClassUriVisibility)
      {
        //Set visibility
        IsVisible = false,
      };
      //Create the layer with the feature layer parameters and add it to the active map
      var createdFC = LayerFactory.Instance.CreateLayer<FeatureLayer>(layerParamsVisibility, MapView.Active.Map);
      #endregion

      #region Create FeatureLayer with a Renderer
      //Note: Call within QueuedTask.Run()
      //Define a simple renderer to draw the Point US Cities feature class.
      var simpleRender = new SimpleRendererDefinition
      {
        SymbolTemplate = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.RedRGB, 4.0, SimpleMarkerStyle.Circle).MakeSymbolReference()

      };
      //The catalog path of the feature layer to add to the map
      var featureClassUri = new Uri(@"C:\Data\Admin\AdminData.gdb\USA\cities");
      //Define the Feature Layer's parameters.
      var layerParams = new FeatureLayerCreationParams(featureClassUri)
      {
        //Set visibility
        IsVisible = true,
        //Set Renderer
        RendererDefinition = simpleRender,
      };
      //Create the layer with the feature layer parameters and add it to the active map
      var createdFCWithRenderer = LayerFactory.Instance.CreateLayer<FeatureLayer>(layerParams, MapView.Active.Map);
      #endregion
      #region Create FeatureLayer with a Query Definition
      //The catalog path of the feature layer to add to the map
      var featureClassUriDefinition = new Uri(@"C:\Data\Admin\AdminData.gdb\USA\cities");
      //Define the Feature Layer's parameters.
      var layerParamsQueryDefn = new FeatureLayerCreationParams(featureClassUriDefinition)
      {
        IsVisible = true,
        DefinitionFilter = new CIMDefinitionFilter()
        {
          Name = "CACities",
          DefinitionExpression = "STATE_NAME = 'California'"
        }

      };
      //Create the layer with the feature layer parameters and add it to the active map
      var createdFCWithQueryDefn = LayerFactory.Instance.CreateLayer<FeatureLayer>(layerParamsQueryDefn, MapView.Active.Map);
      #endregion

      return createdFCWithQueryDefn;
    }

    private async void MapNotesAPI()
    {
      #region Add MapNotes to the active map
      //Gets the collection of layer template packages installed with Pro for use with maps
      var items = MapView.Active.Map.LayerTemplatePackages;     
      //Iterate through the collection of items to add each Map Note to the active map
      foreach (var item in items)
      {
        //Create a parameter item for the map note
        var layer_params = new LayerCreationParams(item);
        layer_params.IsVisible = false;
        await QueuedTask.Run(() => {
          //Create a feature layer for the map note
          var layer = LayerFactory.Instance.CreateLayer<Layer>(layer_params, MapView.Active.Map);
        });
      }
      #endregion
    }

    public static void CreateLayerUsingDocument()
    {
      #region Apply Symbology from a Layer in the TOC
      //Note: Call within QueuedTask.Run()
      if (MapView.Active.Map == null) return;
      //Get an existing Layer. This layer has a symbol you want to use in a new layer.
      var lyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
            .Where(l => l.ShapeType == esriGeometryType.esriGeometryPoint).FirstOrDefault();
      //This is the renderer to use in the new Layer
      var renderer = lyr.GetRenderer() as CIMSimpleRenderer;
      //Set the Dataconnection for the new layer
      Geodatabase geodatabase = new Geodatabase(
        new FileGeodatabaseConnectionPath(new Uri(@"E:\Data\Admin\AdminData.gdb")));
      FeatureClass featureClass = geodatabase.OpenDataset<FeatureClass>("Cities");
      var dataConnection = featureClass.GetDataConnection();
      //Create the definition for the new feature layer
      var featureLayerParams = new FeatureLayerCreationParams(dataConnection)
      {
        RendererDefinition = new SimpleRendererDefinition(renderer.Symbol),
        IsVisible = true,
      };
      //create the new layer
      LayerFactory.Instance.CreateLayer<FeatureLayer>(
        featureLayerParams, MapView.Active.Map, LayerPosition.AutoArrange);
      #endregion
    }
    private void CreateSubTypeLayers()
    {

      #region Create a new SubTypeGroupLayer
      var subtypeGroupLayerCreateParam = new SubtypeGroupLayerCreationParams
      (
          new Uri(@"c:\data\SubtypeAndDomain.gdb\Fittings")
      );

      // Define Subtype layers
      subtypeGroupLayerCreateParam.SubtypeLayers = new List<SubtypeFeatureLayerCreationParams>()
      {
        //define first subtype layer with unique value renderer
        new SubtypeFeatureLayerCreationParams()
        {
          SubtypeId = 1,
          RendererDefinition = new UniqueValueRendererDefinition(new string[] { "type" })
        },

        //define second subtype layer with simple symbol renderer
        new SubtypeFeatureLayerCreationParams()
        {
          SubtypeId = 2,
          RendererDefinition = new SimpleRendererDefinition()
          {
              SymbolTemplate = SymbolFactory.Instance.ConstructPointSymbol(
                CIMColor.CreateRGBColor(255, 0, 0), 8, SimpleMarkerStyle.Hexagon).MakeSymbolReference()
          }
        }
      };

      // Define additional parameters
      subtypeGroupLayerCreateParam.DefinitionFilter = new CIMDefinitionFilter()
      {
        Name = "IsActive",
        DefinitionExpression = "Enabled = 1"
      };
      subtypeGroupLayerCreateParam.IsVisible = true;
      subtypeGroupLayerCreateParam.MinimumScale = 50000;

      SubtypeGroupLayer subtypeGroupLayer2 = LayerFactory.Instance.CreateLayer<SubtypeGroupLayer>(
                    subtypeGroupLayerCreateParam, MapView.Active.Map);
      #endregion
    }

    public async Task CreateLayerFromALyrxFile()
    {
      Map map = null;

      #region Create layer from a lyrx file
      var lyrDocFromLyrxFile = new LayerDocument(@"d:\data\cities.lyrx");
      var cimLyrDoc = lyrDocFromLyrxFile.GetCIMLayerDocument();

      //modifying its renderer symbol to red
      var r = ((CIMFeatureLayer)cimLyrDoc.LayerDefinitions[0]).Renderer as CIMSimpleRenderer;
      r.Symbol.Symbol.SetColor(new CIMRGBColor() { R = 255 });

      //optionally save the updates out as a file
      lyrDocFromLyrxFile.Save(@"c:\data\cities_red.lyrx");

      //get a json representation of the layer document and you want store away...
      var aJSONString = lyrDocFromLyrxFile.AsJson();

      //... and load it back when needed
      lyrDocFromLyrxFile.Load(aJSONString);
      cimLyrDoc = lyrDocFromLyrxFile.GetCIMLayerDocument();

      //create a layer and add it to a map
      var lcp = new LayerCreationParams(cimLyrDoc);
      var lyr = LayerFactory.Instance.CreateLayer<FeatureLayer>(lcp, map, LayerPosition.AutoArrange);
      #endregion
    }
    private static async Task ModifyLayerSymbologyFromLyrFileAsync(IEnumerable<FeatureLayer> featureLayers, string layerFile)
    {
      await QueuedTask.Run(() =>
      {
        foreach (var featureLayer in featureLayers)
        {
          #region Apply Symbology to a layer from a Layer file
          //Note: Run within QueuedTask.Run
          //Get the Layer Document from the lyrx file
          var lyrDocFromLyrxFile = new LayerDocument(layerFile);
          var cimLyrDoc = lyrDocFromLyrxFile.GetCIMLayerDocument();

          //Get the renderer from the layer file
          var rendererFromLayerFile = ((CIMFeatureLayer)cimLyrDoc.LayerDefinitions[0]).Renderer as CIMUniqueValueRenderer;

          //Apply the renderer to the feature layer
          //Note: If working with a raster layer, use the SetColorizer method.
          featureLayer?.SetRenderer(rendererFromLayerFile);
          #endregion
        }

      });
    }


    public async Task AddWMSLayerAsync()
    {
      {
        #region Add a WMS service
        // Create a connection to the WMS server
        var serverConnection = new CIMInternetServerConnection { URL = "URL of the WMS service" };
        var connection = new CIMWMSServiceConnection { ServerConnection = serverConnection };

        // Add a new layer to the map
        await QueuedTask.Run(() =>
        {
          var layer = LayerFactory.Instance.CreateLayer(connection, MapView.Active.Map);
        });
        #endregion
      }
      {

        #region Add a WFS Service
        CIMStandardDataConnection cIMStandardDataConnection = new CIMStandardDataConnection()
        {
          WorkspaceConnectionString = @"SWAPXY=TRUE;SWAPXYFILTER=FALSE;URL=http://sampleserver6.arcgisonline.com/arcgis/services/SampleWorldCities/MapServer/WFSServer;VERSION=2.0.0",
          WorkspaceFactory = WorkspaceFactory.WFS,
          Dataset = "Continent",
          DatasetType = esriDatasetType.esriDTFeatureClass
        };

        // Add a new layer to the map
        await QueuedTask.Run(() =>
        {
          Layer layer = LayerFactory.Instance.CreateLayer(cIMStandardDataConnection, MapView.Active.Map);
        });
        #endregion

      }
    }
    public async Task AddQuerylayerAsync()
    {
      #region Create a query layer
      await QueuedTask.Run(() =>
      {
        Map map = MapView.Active.Map;
        Geodatabase geodatabase = new Geodatabase(new DatabaseConnectionFile(new Uri(@"C:\Connections\mySDE.sde")));
        CIMSqlQueryDataConnection sqldc = new CIMSqlQueryDataConnection()
        {
          WorkspaceConnectionString = geodatabase.GetConnectionString(),
          GeometryType = esriGeometryType.esriGeometryPolygon,
          OIDFields = "OBJECTID",
          Srid = "102008",
          SqlQuery = "select * from MySDE.dbo.STATES",
          Dataset = "States"
        };
        FeatureLayer flyr = (FeatureLayer)LayerFactory.Instance.CreateLayer(sqldc, map, layerName: "States");
      });
      #endregion
    }
    public async Task AddFeatureLayerClasBreaksAsync()
    {

      #region Create a feature layer with class breaks renderer with defaults
      await QueuedTask.Run(() =>
        LayerFactory.Instance.CreateFeatureLayer(
          new Uri(@"c:\data\countydata.gdb\counties"),
          MapView.Active.Map,
          layerName: "Population Density (sq mi) Year 2010",
          rendererDefinition: new GraduatedColorsRendererDefinition("POP10_SQMI")
        )
      );
      #endregion
    }

    public async Task AddFeatureLayerClasBreaksExAsync()
    {

      #region Create a feature layer with class breaks renderer

      string colorBrewerSchemesName = "ColorBrewer Schemes (RGB)";
      StyleProjectItem style = Project.Current.GetItems<StyleProjectItem>().First(s => s.Name == colorBrewerSchemesName);
      string colorRampName = "Greens (Continuous)";
      IList<ColorRampStyleItem> colorRampList = await QueuedTask.Run(() =>
      {
        return style.SearchColorRamps(colorRampName);
      });
      ColorRampStyleItem colorRamp = colorRampList[0];

      await QueuedTask.Run(() =>
      {
        GraduatedColorsRendererDefinition gcDef = new GraduatedColorsRendererDefinition()
        {
          ClassificationField = "CROP_ACR07",
          ClassificationMethod = ArcGIS.Core.CIM.ClassificationMethod.NaturalBreaks,
          BreakCount = 6,
          ColorRamp = colorRamp.ColorRamp,
          SymbolTemplate = SymbolFactory.Instance.ConstructPolygonSymbol(
                                  ColorFactory.Instance.GreenRGB, SimpleFillStyle.Solid, null).MakeSymbolReference(),
          ExclusionClause = "CROP_ACR07 = -99",
          ExclusionSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(
                                  ColorFactory.Instance.RedRGB, SimpleFillStyle.Solid, null).MakeSymbolReference(),
          ExclusionLabel = "No yield",
        };

        LayerFactory.Instance.CreateFeatureLayer(new Uri(@"c:\Data\CountyData.gdb\Counties"),
                  MapView.Active.Map, layerName: "Crop", rendererDefinition: gcDef);
      });

      #endregion
    }
    #region ProSnippet Group: Basemap Layers
    #endregion

    private void BaseMap()
    {
      Map aMap = null;

      #region Update a map's basemap layer
      aMap.SetBasemapLayers(Basemap.Gray);
      #endregion

      #region Remove basemap layer from a map
      aMap.SetBasemapLayers(Basemap.None);
      #endregion    
    }

    #region ProSnippet Group: Working with Layers
    #endregion

    public void Simple()
    {
      Map aMap = null;

      #region Get a list of layers filtered by layer type from a map
      List<FeatureLayer> featureLayerList = aMap.GetLayersAsFlattenedList().OfType<FeatureLayer>().ToList();
      #endregion

      #region Find a layer
      //Finds layers by name and returns a read only list of Layers
      IReadOnlyList<Layer> layers = aMap.FindLayers("cities", true);

      //Finds a layer using a URI.
      //The Layer URI you pass in helps you search for a specific layer in a map
      var lyrFindLayer = MapView.Active.Map.FindLayer("CIMPATH=map/u_s__states__generalized_.xml");

      //This returns a collection of layers of the "name" specified. You can use any Linq expression to query the collection.  
      var lyrExists = MapView.Active.Map.GetLayersAsFlattenedList()
                         .OfType<FeatureLayer>().Any(f => f.Name == "U.S. States (Generalized)");
      #endregion

      #region Find a standalone table
      IReadOnlyList<StandaloneTable> tables = aMap.FindStandaloneTables("addresses");
      #endregion    
    }
    public List<Layer> FindLayersWithPartialName(string partialName)
    {

      #region Find a layer using partial name search

      Map map = MapView.Active.Map;
      IEnumerable<Layer> matches = map.GetLayersAsFlattenedList().Where(l => l.Name.IndexOf(partialName, StringComparison.CurrentCultureIgnoreCase) >= 0);

      #endregion

      List<Layer> layers = new List<Layer>();
      foreach (Layer l in matches)
        layers.Add(l);

      return layers;
    }

    public void CreateLyrx()
    {
      Layer layer = null;
      #region Create a Lyrx file

      LayerDocument layerDocument = new LayerDocument(layer);
      layerDocument.Save(@"c:\Data\MyLayerDocument.lyrx");
      #endregion
    }
    public void MiscOneLiners()
    {
      Map aMap = MapView.Active.Map;

      #region Count the features selected in a map
      var lyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      var noFeaturesSelected = lyr.SelectionCount;
      #endregion
    }

    public void AccessDisplayField()
    {
      #region Access the display field for a layer
      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
      {
        // get the CIM definition from the layer
        var cimFeatureDefinition = featureLayer.GetDefinition() as ArcGIS.Core.CIM.CIMBasicFeatureLayer;
        // get the view of the source table underlying the layer
        var cimDisplayTable = cimFeatureDefinition.FeatureTable;
        // this field is used as the 'label' to represent the row
        var displayField = cimDisplayTable.DisplayField;
      });
      #endregion
    }

    public void EnableLabeling()
    {
      #region Enable labeling on a layer
      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
      {
        // toggle the label visibility
        featureLayer.SetLabelVisibility(!featureLayer.IsLabelVisible);
      });
      #endregion
    }

    public static void MoveLayerTo3D()
    {
      #region Move a layer in the 2D group to the 3D Group in a Local Scene
      //The layer in the 2D group to move to the 3D Group in a Local Scene
      var layer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      QueuedTask.Run(() =>
      {
        //Get the layer's definition
        var lyrDefn = layer.GetDefinition() as CIMBasicFeatureLayer;
        //setting this property moves the layer to 3D group in a scene
        lyrDefn.IsFlattened = false;
        //Set the definition back to the layer
        layer.SetDefinition(lyrDefn);
      });
      #endregion
    }

    private static void ResetDataConnectionFeatureService(Layer dataConnectionLayer, string newConnectionString)
    {
      #region Reset the URL of a feature service layer 
      CIMStandardDataConnection dataConnection = dataConnectionLayer.GetDataConnection() as CIMStandardDataConnection;
      dataConnection.WorkspaceConnectionString = newConnectionString;
      dataConnectionLayer.SetDataConnection(dataConnection);
      #endregion
    }
    private static void ReplaceDataSource()
    {
      #region Change the underlying data source of a feature layer - same workspace type
      //This is the existing layer for which we want to switch the underlying datasource
      var lyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      QueuedTask.Run(() =>
      {
        var connectionStringToReplace = lyr.GetFeatureClass().GetDatastore().GetConnectionString();
        string databaseConnectionPath = @"Path to the .sde connection file to replace with";
        //If the new SDE connection did not have a dataset with the same name as in the feature layer,
        //pass false for the validate parameter of the FindAndReplaceWorkspacePath method to achieve this. 
        //If validate is true and the SDE did not have a dataset with the same name, 
        //FindAndReplaceWorkspacePath will return failure
        lyr.FindAndReplaceWorkspacePath(connectionStringToReplace, databaseConnectionPath, true);
      });
      #endregion
    }

    public async Task ChangeGDBVersion2Async()
    {

      #region Change Geodatabase Version of layers off a specified version in a map

      await QueuedTask.Run(() =>
      {
              //Getting the current version name from the first feature layer of the map
              FeatureLayer flyr = MapView.Active.Map.GetLayersAsFlattenedList()
                      .OfType<FeatureLayer>().FirstOrDefault();  //first feature layer
              Datastore dataStore = flyr.GetFeatureClass().GetDatastore();  //getting datasource
              Geodatabase geodatabase = dataStore as Geodatabase; //casting to Geodatabase
              if (geodatabase == null)
          return;

        VersionManager versionManager = geodatabase.GetVersionManager();
        ArcGIS.Core.Data.Version currentVersion = versionManager.GetCurrentVersion();

              //Getting all available versions except the current one
              IEnumerable<ArcGIS.Core.Data.Version> versions = versionManager.GetVersions()
                      .Where(v => !v.GetName().Equals(currentVersion.GetName(), StringComparison.CurrentCultureIgnoreCase));

              //Assuming there is at least one other version we pick the first one from the list
              ArcGIS.Core.Data.Version toVersion = versions.FirstOrDefault();
        if (toVersion != null)
        {
                //Changing version
                MapView.Active.Map.ChangeVersion(currentVersion, toVersion);
        }
      });
      #endregion

    }
    public async void SearchAndGetFeatureCount()
    {

      #region Querying a feature layer

      var count = await QueuedTask.Run(() =>
      {
        QueryFilter qf = new QueryFilter()
        {
          WhereClause = "Class = 'city'"
        };

              //Getting the first selected feature layer of the map view
              var flyr = (FeatureLayer)MapView.Active.GetSelectedLayers()
                                .OfType<FeatureLayer>().FirstOrDefault();
        using (RowCursor rows = flyr.Search(qf)) //execute
              {
                //Looping through to count
                int i = 0;
          while (rows.MoveNext()) i++;

          return i;
        }
      });
      MessageBox.Show(String.Format(
         "Total features that matched the search criteria: {0}", count));

      #endregion

    }

    public static void GetRotationFieldOfRenderer()
    {
      #region Get the attribute rotation field of a layer
      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      QueuedTask.Run(() =>
      {
        var cimRenderer = featureLayer.GetRenderer() as CIMUniqueValueRenderer;
        var cimRotationVariable = cimRenderer.VisualVariables.OfType<CIMRotationVisualVariable>().FirstOrDefault();
        var rotationInfoZ = cimRotationVariable.VisualVariableInfoZ;
        var rotationExpression = rotationInfoZ.ValueExpressionInfo.Expression; // this expression stores the field name  
            });
      #endregion
    }

    public void FindConnectedAttribute()
    {
      #region Find connected attribute field for rotation
      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
      {
              // get the CIM renderer from the layer
              var cimRenderer = featureLayer.GetRenderer() as ArcGIS.Core.CIM.CIMSimpleRenderer;
              // get the collection of connected attributes for rotation
              var cimRotationVariable = cimRenderer.VisualVariables.OfType<ArcGIS.Core.CIM.CIMRotationVisualVariable>().FirstOrDefault();
              // the z direction is describing the heading rotation
              var rotationInfoZ = cimRotationVariable.VisualVariableInfoZ;
        var rotationExpression = rotationInfoZ.Expression; // this expression stores the field name  
            });
      #endregion
    }

    public void ScaleSymbols()
    {
      #region Toggle "Scale layer symbols when reference scale is set"
      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
      {
              // get the CIM layer definition
              var cimFeatureLayer = featureLayer.GetDefinition() as ArcGIS.Core.CIM.CIMFeatureLayer;
              // turn on the option to scale the symbols in this layer based in the map's reference scale
              cimFeatureLayer.ScaleSymbols = true;
      });
      #endregion
    }

    public void SetLayerCache()
    {
      #region Set the layer cache
      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
      {
              // change the layer cache type to maximum age
              featureLayer.SetDisplayCacheType(ArcGIS.Core.CIM.DisplayCacheType.MaxAge);
              // change from the default 5 min to 2 min
              featureLayer.SetDisplayCacheMaxAge(TimeSpan.FromMinutes(2));
      });
      #endregion
    }

    public void ChangeSelectionColor()
    {
      #region Change the layer selection color
      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
      {
              // get the CIM definition of the layer
              var layerDef = featureLayer.GetDefinition() as ArcGIS.Core.CIM.CIMBasicFeatureLayer;
              // disable the default symbol
              layerDef.UseSelectionSymbol = false;
              // assign a new color
              layerDef.SelectionColor = ColorFactory.Instance.RedRGB;
              // apply the definition to the layer
              featureLayer.SetDefinition(layerDef);

        if (!featureLayer.IsVisible) featureLayer.SetVisibility(true);
              //Do a selection

              MapView.Active.SelectFeatures(MapView.Active.Extent);
      });
      #endregion
    }
    public async void RemoveAllUncheckedLayers()
    {
      #region Removes all layers that are unchecked
      var map = MapView.Active.Map;
      if (map == null)
        return;
      //Get the group layers first
      IReadOnlyList<GroupLayer> groupLayers = map.Layers.OfType<GroupLayer>().ToList();
      //Iterate and remove the layers within the group layers that are unchecked.
      foreach (var groupLayer in groupLayers)
      {
        //Get layers that not visible within the group
        var layers = groupLayer.Layers.Where(l => l.IsVisible == false).ToList();
        //Remove all the layers that are not visible within the group
        await QueuedTask.Run(() => map.RemoveLayers(layers));
      }

      //Group Layers that are empty and are unchecked
      foreach (var group in groupLayers)
      {
        if (group.Layers.Count == 0 && group.IsVisible == false) //No layers in the group
        {
          //remove the group
          await QueuedTask.Run(() => map.RemoveLayer(group));
        }
      }

      //Get Layers that are NOT Group layers and are unchecked
      var notAGroupAndUnCheckedLayers = map.Layers.Where(l => !(l is GroupLayer) && l.IsVisible == false).ToList();
      //Remove all the non group layers that are not visible
      await QueuedTask.Run(() => map.RemoveLayers(notAGroupAndUnCheckedLayers));
      #endregion

    }
    public async void RemoveEmptyGroups()
    {
      #region Remove empty groups
      var map = MapView.Active.Map;
      if (map == null)
        return;
      //Get the group layers
      IReadOnlyList<GroupLayer> groupLayers = map.Layers.OfType<GroupLayer>().ToList();
      foreach (var group in groupLayers)
      {
        if (group.Layers.Count == 0) //No layers in the group
        {
          //remove the group
          await QueuedTask.Run(() => map.RemoveLayer(group));
        }
      }
      #endregion

    }
    #region Create and apply Abbreviation Dictionary in the Map Definition to a layer
    public static void CreateDictionary()
    {
      //Get the map's defintion
      var mapDefn = MapView.Active.Map.GetDefinition();
      //Get the Map's Maplex labelling engine properties
      var mapDefnPlacementProps = mapDefn.GeneralPlacementProperties as CIMMaplexGeneralPlacementProperties;

      //Define the abbreaviations we need in an array            
      List<CIMMaplexDictionaryEntry> abbreviationDictionary = new List<CIMMaplexDictionaryEntry>
            {
                new CIMMaplexDictionaryEntry {
                Abbreviation = "Hts",
                Text = "Heights",
                MaplexAbbreviationType = MaplexAbbreviationType.Ending

             },
                new CIMMaplexDictionaryEntry
                {
                    Abbreviation = "Ct",
                    Text = "Text",
                    MaplexAbbreviationType = MaplexAbbreviationType.Ending

                }
                //etc
            };
      //The Maplex Dictionary - can hold multiple Abbreviation collections
      var maplexDictionary = new List<CIMMaplexDictionary>
            {
                new CIMMaplexDictionary {
                    Name = "NameEndingsAbbreviations",
                    MaplexDictionary = abbreviationDictionary.ToArray()
                }

            };
      //Set the Maplex Label Engine Dictionary property to the Maplex Dictionary collection created above.
      mapDefnPlacementProps.Dictionaries = maplexDictionary.ToArray();
      //Set the Map defintion 
      MapView.Active.Map.SetDefinition(mapDefn);
    }

    private static void ApplyDictionary()
    {
      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().First();

      QueuedTask.Run(() =>
      {
        //Creates Abbreviation dictionary and adds to Map Defintion                                
        CreateDictionary();
        //Get the layer's definition
        var lyrDefn = featureLayer.GetDefinition() as CIMFeatureLayer;
        //Get the label classes - we need the first one
        var listLabelClasses = lyrDefn.LabelClasses.ToList();
        var theLabelClass = listLabelClasses.FirstOrDefault();
        //Modify label Placement props to use abbreviation dictionary 
        CIMGeneralPlacementProperties labelEngine = MapView.Active.Map.GetDefinition().GeneralPlacementProperties;
        theLabelClass.MaplexLabelPlacementProperties.DictionaryName = "NameEndingsAbbreviations";
        theLabelClass.MaplexLabelPlacementProperties.CanAbbreviateLabel = true;
        theLabelClass.MaplexLabelPlacementProperties.CanStackLabel = false;
        //Set the labelClasses back
        lyrDefn.LabelClasses = listLabelClasses.ToArray();
        //set the layer's definition
        featureLayer.SetDefinition(lyrDefn);
      });
    }


    #endregion

    #region ProSnippet Group: Attribute Table - ITablePane
    #endregion

    public void SetTablePaneZoom()
    {
      #region Set zoom level for Attribute Table
      if (FrameworkApplication.Panes.ActivePane is ITablePane tablePane)
      {
        var currentZoomLevel = tablePane.ZoomLevel;

        var newZoomLevel = currentZoomLevel + 50;
        tablePane.SetZoomLevel(newZoomLevel);
      }

      #endregion
    }

    public static Task<object> ActiveCellContents()
    {
      #region Retrieve the values of selected cell in the attribute table
      if (FrameworkApplication.Panes.ActivePane is ITablePane tablePane)
      {
        var mapMember = tablePane.MapMember;
        var oid = tablePane.ActiveObjectID;
        if (oid.HasValue && oid.Value != -1 && mapMember != null)
        {
          var activeField = tablePane.ActiveColumn;
          return QueuedTask.Run<object>(() =>
          {
            // TODO: Use core objects to retrieve record and get value

            return null;
          });
        }
      }
      #endregion
      return Task.FromResult<object>(null);
    }

    #region ProSnippet Group: Metadata
    #endregion
    private void MapLayerMetadata()
    {
      #region Get and Set Map Metadata
      var map = MapView.Active.Map;
      if (map == null) return;
      //Get map's metadata
      var mapMetadata = map.GetMetadata();
      //TODO:Make edits to metadata using the retrieved mapMetadata string.

      //Set the modified metadata back to the map.
      if (map.GetCanEditMetadata())
        map.SetMetadata(mapMetadata);
      #endregion

      #region Layer Metadata
      MapMember mapMember = map.GetLayersAsFlattenedList().FirstOrDefault(); //Search for only layers/tables here if needed.
      if (mapMember == null) return;

      //Gets whether or not the MapMember stores its own metadata or uses metadata retrieved
      //from its source. This method must be called on the MCT. Use QueuedTask.Run
      bool doesUseSourceMetadata = mapMember.GetUseSourceMetadata();

      //Sets whether or not the MapMember will use its own metadata or the metadata from
      //its underyling source (if it has one). This method must be called on the MCT.
      //Use QueuedTask.Run
      mapMember.SetUseSourceMetadata(true);

      //Does the MapMember supports metadata
      var supportsMetadata = mapMember.SupportsMetadata;

      //Get MapMember metadata
      var metadatstring = mapMember.GetMetadata();
      //TODO:Make edits to metadata using the retrieved mapMetadata string.

      //Set the modified metadata back to the mapmember (layer, table..)
      if (mapMember.GetCanEditMetadata())
        mapMember.SetMetadata(metadatstring);
      #endregion
    }

    #region ProSnippet Group: Renderers
    #endregion
    public async Task SetUniqueValueRendererAsync()
    {


      #region Set unique value renderer to the selected feature layer of the active map

      await QueuedTask.Run(() =>
      {
        String[] fields = new string[] { "Type" }; //field to be used to retrieve unique values
        CIMPointSymbol pointSym = SymbolFactory.Instance.ConstructPointSymbol(
                  ColorFactory.Instance.GreenRGB, 16.0, SimpleMarkerStyle.Pushpin);  //constructing a point symbol as a template symbol
        CIMSymbolReference symbolPointTemplate = pointSym.MakeSymbolReference();

        //constructing renderer definition for unique value renderer
        UniqueValueRendererDefinition uniqueValueRendererDef = new UniqueValueRendererDefinition(fields, symbolPointTemplate);

        //creating a unique value renderer
        var flyr = MapView.Active.GetSelectedLayers()[0] as FeatureLayer;
        CIMUniqueValueRenderer uniqueValueRenderer = (CIMUniqueValueRenderer)flyr.CreateRenderer(uniqueValueRendererDef);

        //setting the renderer to the feature layer
        flyr.SetRenderer(uniqueValueRenderer);
      });
      #endregion

    }

    internal static Task UniqueValueRenderer(FeatureLayer featureLayer)
    {
      #region Create a UniqueValueRenderer to specify symbols to values 
      return QueuedTask.Run(() =>
      {
              //The goal is to construct the CIMUniqueValueRenderer which will be applied to the feature layer.
              // To do this, the following are the objects we need to set the renderer up with the fields and symbols.
              // As a reference, this is the USCities dataset. Snippet will create a unique value renderer that applies 
              // specific symbols to all the cities in California and Alabama.  The rest of the cities will use a default symbol.

              // First create a "CIMUniqueValueClass" for the cities in Alabama.
              List<CIMUniqueValue> listUniqueValuesAlabama = new List<CIMUniqueValue> { new CIMUniqueValue { FieldValues = new string[] { "Alabama" } } };
        CIMUniqueValueClass alabamaUniqueValueClass = new CIMUniqueValueClass
        {
          Editable = true,
          Label = "Alabama",
          Patch = PatchShape.Default,
          Symbol = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.RedRGB).MakeSymbolReference(),
          Visible = true,
          Values = listUniqueValuesAlabama.ToArray()

        };
              // Create a "CIMUniqueValueClass" for the cities in California.
              List<CIMUniqueValue> listUniqueValuescalifornia = new List<CIMUniqueValue> { new CIMUniqueValue { FieldValues = new string[] { "California" } } };
        CIMUniqueValueClass californiaUniqueValueClass = new CIMUniqueValueClass
        {
          Editable = true,
          Label = "California",
          Patch = PatchShape.Default,
          Symbol = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.BlueRGB).MakeSymbolReference(),
          Visible = true,
          Values = listUniqueValuescalifornia.ToArray()
        };
              //Create a list of the above two CIMUniqueValueClasses
              List<CIMUniqueValueClass> listUniqueValueClasses = new List<CIMUniqueValueClass>
          {
                        alabamaUniqueValueClass, californiaUniqueValueClass
          };
              //Create a list of CIMUniqueValueGroup
              CIMUniqueValueGroup uvg = new CIMUniqueValueGroup
        {
          Classes = listUniqueValueClasses.ToArray(),
        };
        List<CIMUniqueValueGroup> listUniqueValueGroups = new List<CIMUniqueValueGroup> { uvg };
              //Create the CIMUniqueValueRenderer
              CIMUniqueValueRenderer uvr = new CIMUniqueValueRenderer
        {
          UseDefaultSymbol = true,
          DefaultLabel = "all other values",
          DefaultSymbol = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.GreyRGB).MakeSymbolReference(),
          Groups = listUniqueValueGroups.ToArray(),
          Fields = new string[] { "STATE_NAME" }
        };
              //Set the feature layer's renderer.
              featureLayer.SetRenderer(uvr);
      });
      #endregion
    }
    public async void CreateHeatMapRenderer()
    {
      #region Create a Heatmap Renderer
      string colorBrewerSchemesName = "ArcGIS Colors";
      StyleProjectItem style = Project.Current.GetItems<StyleProjectItem>().First(s => s.Name == colorBrewerSchemesName);
      string colorRampName = "Heat Map 4 - Semitransparent";
      IList<ColorRampStyleItem> colorRampList = await QueuedTask.Run(() =>
      {
        return style.SearchColorRamps(colorRampName);
      });
      ColorRampStyleItem colorRamp = colorRampList[0];

      await QueuedTask.Run(() =>
      {
              //defining a heatmap renderer that uses values from Population field as the weights
              HeatMapRendererDefinition heatMapDef = new HeatMapRendererDefinition()
        {
          Radius = 20,
          WeightField = "Population",
          ColorRamp = colorRamp.ColorRamp,
          RendereringQuality = 8,
          UpperLabel = "High Density",
          LowerLabel = "Low Density"
        };

        FeatureLayer flyr = MapView.Active.Map.Layers[0] as FeatureLayer;
        CIMHeatMapRenderer heatMapRndr = (CIMHeatMapRenderer)flyr.CreateRenderer(heatMapDef);
        flyr.SetRenderer(heatMapRndr);
      });
      #endregion
    }

    public async void CreateUnclassedRenderer()
    {
      #region Create an Unclassed Renderer
      string colorBrewerSchemesName = "ArcGIS Colors";
      StyleProjectItem style = Project.Current.GetItems<StyleProjectItem>().First(s => s.Name == colorBrewerSchemesName);
      string colorRampName = "Heat Map 4 - Semitransparent";
      IList<ColorRampStyleItem> colorRampList = await QueuedTask.Run(() =>
      {
        return style.SearchColorRamps(colorRampName);
      });
      ColorRampStyleItem colorRamp = colorRampList[0];

      await QueuedTask.Run(() =>
      {
        CIMPointSymbol pointSym = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.GreenRGB, 16.0, SimpleMarkerStyle.Diamond);
        CIMSymbolReference symbolPointTemplate = pointSym.MakeSymbolReference();

              //defining an unclassed renderer with custom upper and lower stops
              //all features with value >= 5,000,000 will be drawn with the upper color from the color ramp
              //all features with value <= 50,000 will be drawn with the lower color from the color ramp
              UnclassedColorsRendererDefinition unclassRndrDef = new UnclassedColorsRendererDefinition
                                          ("Population", symbolPointTemplate, colorRamp.ColorRamp, "Highest", "Lowest", 5000000, 50000)
        {

                //drawing features with null values with a different symbol
                ShowNullValues = true,
          NullValueLabel = "Unknown"
        };
        CIMPointSymbol nullSym = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.RedRGB, 16.0, SimpleMarkerStyle.Circle);
        unclassRndrDef.NullValueSymbol = nullSym.MakeSymbolReference();
        FeatureLayer flyr = MapView.Active.Map.Layers[0] as FeatureLayer;
        CIMClassBreaksRenderer cbRndr = (CIMClassBreaksRenderer)flyr.CreateRenderer(unclassRndrDef);
        flyr.SetRenderer(cbRndr);
      });
      #endregion
    }

    public async void CreateProportionaRenderer()
    {
      #region Create a Proportion Renderer with max and min symbol size capped
      string colorBrewerSchemesName = "ArcGIS Colors";
      StyleProjectItem style = Project.Current.GetItems<StyleProjectItem>().First(s => s.Name == colorBrewerSchemesName);
      string colorRampName = "Heat Map 4 - Semitransparent";
      IList<ColorRampStyleItem> colorRampList = await QueuedTask.Run(() =>
      {
        return style.SearchColorRamps(colorRampName);
      });
      ColorRampStyleItem colorRamp = colorRampList[0];

      await QueuedTask.Run(() =>
      {
        CIMPointSymbol pointSym = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.GreenRGB, 1.0, SimpleMarkerStyle.Circle);
        CIMSymbolReference symbolPointTemplate = pointSym.MakeSymbolReference();

              //minimum symbol size is capped to 4 point while the maximum symbol size is set to 50 point
              ProportionalRendererDefinition prDef = new ProportionalRendererDefinition("POPULATION", symbolPointTemplate, 4, 50, true)
        {

                //setting upper and lower size stops to stop symbols growing or shrinking beyond those thresholds
                UpperSizeStop = 5000000,  //features with values >= 5,000,000 will be drawn with maximum symbol size
                LowerSizeStop = 50000    //features with values <= 50,000 will be drawn with minimum symbol size
              };
        FeatureLayer flyr = MapView.Active.Map.Layers[0] as FeatureLayer;
        CIMProportionalRenderer propRndr = (CIMProportionalRenderer)flyr.CreateRenderer(prDef);
        flyr.SetRenderer(propRndr);

      });
      #endregion
    }

    public async void CreateTrueProportionaRenderer()
    {
      #region Create a True Proportion Renderer
      string colorBrewerSchemesName = "ArcGIS Colors";
      StyleProjectItem style = Project.Current.GetItems<StyleProjectItem>().First(s => s.Name == colorBrewerSchemesName);
      string colorRampName = "Heat Map 4 - Semitransparent";
      IList<ColorRampStyleItem> colorRampList = await QueuedTask.Run(() =>
      {
        return style.SearchColorRamps(colorRampName);
      });
      ColorRampStyleItem colorRamp = colorRampList[0];

      await QueuedTask.Run(() =>
      {
        CIMPointSymbol pointSym = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.GreenRGB, 1.0, SimpleMarkerStyle.Circle);
        CIMSymbolReference symbolPointTemplate = pointSym.MakeSymbolReference();

              //Defining proportional renderer where size of symbol will be same as its value in field used in the renderer.
              ProportionalRendererDefinition prDef = new ProportionalRendererDefinition("POPULATION", esriUnits.esriMeters, symbolPointTemplate, SymbolShapes.Square, ValueRepresentations.Radius);

        FeatureLayer flyr = MapView.Active.Map.Layers[0] as FeatureLayer;
        CIMProportionalRenderer propRndr = (CIMProportionalRenderer)flyr.CreateRenderer(prDef);
        flyr.SetRenderer(propRndr);

      });
      #endregion
    }

    #region ProSnippet Group: Arcade
    #endregion

    protected static void ArcadeRenderer()
    {
      #region Modify renderer using Arcade
      var lyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(f => f.ShapeType == esriGeometryType.esriGeometryPolygon);
      if (lyr == null) return;
      QueuedTask.Run(() =>
      {
        // GetRenderer from Layer (assumes it is a unique value renderer)
        var uvRenderer = lyr.GetRenderer() as CIMUniqueValueRenderer;
        if (uvRenderer == null) return;
        //layer has STATE_NAME field
        //community sample Data\Admin\AdminSample.aprx
        string expression = "if ($view.scale > 21000000) { return $feature.STATE_NAME } else { return 'All' }";
        CIMExpressionInfo updatedExpressionInfo = new CIMExpressionInfo
        {
          Expression = expression,
          Title = "Custom" // can be any string used for UI purpose.
        };
        //set the renderer's expression
        uvRenderer.ValueExpressionInfo = updatedExpressionInfo;

        //SetRenderer on Layer
        lyr.SetRenderer(uvRenderer);
      });
      #endregion
    }
    protected static void ArcadeLabeling()
    {
      #region Modify label expression using Arcade
      var lyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(f => f.ShapeType == esriGeometryType.esriGeometryPolygon);
      if (lyr == null) return;
      QueuedTask.Run(() =>
      {
        //Get the layer's definition
        //community sample Data\Admin\AdminSample.aprx
        var lyrDefn = lyr.GetDefinition() as CIMFeatureLayer;
        if (lyrDefn == null) return;
        //Get the label classes - we need the first one
        var listLabelClasses = lyrDefn.LabelClasses.ToList();
        var theLabelClass = listLabelClasses.FirstOrDefault();
        //set the label class Expression to use the Arcade expression
        theLabelClass.Expression = "return $feature.STATE_NAME + TextFormatting.NewLine + $feature.POP2000;";
        //Set the label definition back to the layer.
        lyr.SetDefinition(lyrDefn);
      });

      #endregion
    }

    #region ProSnippet Group: Elevation Surface
    #endregion

    private Task CreateNewElevationSurface()
    {
      return ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
      {
        #region Create a New Elevation Surface
        //Note: call within QueuedTask.Run()
        //Define a ServiceConnection to use for the new Elevation surface
        var serverConnection = new CIMInternetServerConnection
        {
          Anonymous = true,
          HideUserProperty = true,
          URL = "https://elevation.arcgis.com/arcgis/services"
        };
        CIMAGSServiceConnection serviceConnection = new CIMAGSServiceConnection
        {
          ObjectName = "WorldElevation/Terrain",
          ObjectType = "ImageServer",
          URL = "https://elevation.arcgis.com/arcgis/services/WorldElevation/Terrain/ImageServer",
          ServerConnection = serverConnection
        };
        //Defines a new elevation source set to the CIMAGSServiceConnection defined above
        var newElevationSource = new ArcGIS.Core.CIM.CIMElevationSource
        {
          VerticalUnit = ArcGIS.Core.Geometry.LinearUnit.Meters,
          DataConnection = serviceConnection,
          Name = "WorldElevation/Terrain",
          Visibility = true
        };
        //The elevation surface
        var newElevationSurface = new ArcGIS.Core.CIM.CIMMapElevationSurface
        {
          Name = "New Elevation Surface",
          BaseSources = new ArcGIS.Core.CIM.CIMElevationSource[1] { newElevationSource },
          Visibility = true,
          ElevationMode = ElevationMode.CustomSurface,
          VerticalExaggeration = 1,
          EnableSurfaceShading = false,
          SurfaceTINShadingMode = SurfaceTINShadingMode.Smooth,
          Expanded = false,
          MapElevationID = "{3DEC3CC5-7C69-4132-A700-DCD5BDED14D6}"
        };
        //Get the active map
        var map = MapView.Active.Map;
        //Get the active map's definition
        var definition = map.GetDefinition();
        //Get the elevation surfaces defined in the map
        var listOfElevationSurfaces = definition.ElevationSurfaces.ToList();
        //Add the new elevation surface 
        listOfElevationSurfaces.Add(newElevationSurface);
        //Set the map definitions to ElevationSurface (this has the new elevation surface)
        definition.ElevationSurfaces = listOfElevationSurfaces.ToArray();
        //Set the map definition
        map.SetDefinition(definition);
        #endregion
      });
    }

    private Task SetElevationSurfaceToLayer(FeatureLayer featureLayer)
    {
      return QueuedTask.Run(() =>
      {
              #region Set a custom elevation surface to a Z-Aware layer

              //Define the custom elevation surface to use
              var layerElevationSurface = new CIMLayerElevationSurface
        {
          MapElevationID = "{3DEC3CC5-7C69-4132-A700-DCD5BDED14D6}"
        };
              //Get the layer's definition
              var lyrDefn = featureLayer.GetDefinition() as CIMBasicFeatureLayer;
              //Set the layer's elevation surface
              lyrDefn.LayerElevation = layerElevationSurface;
              //Set the layer's definition
              featureLayer.SetDefinition(lyrDefn);
              #endregion
            });
    }

    private static async Task<SurfaceZsResult> GetZValue()
    {
      #region Get Z values from a surface
      var geometry = await QueuedTask.Run<Geometry>(() =>
      {
        Geometry mapCentergeometry = MapView.Active.Map.CalculateFullExtent().Center;
        return mapCentergeometry;
      });
      //Pass any Geometry type to GetZsFromSurfaceAsync
      var surfaceZResult = await MapView.Active.Map.GetZsFromSurfaceAsync(geometry);
      return surfaceZResult;
      #endregion
    }
    #region ProSnippet Group: Raster Layers
    #endregion


    public async Task RasterLayers()
    {
      Map aMap = MapView.Active.Map;
      string layerName = null;
      CIMColorRamp colorRamp = null;
      RasterLayer rasterLayer = null;

      #region Create a raster layer
      string url = @"C:\Images\Italy.tif";
      await QueuedTask.Run(() =>
      {
        // Create a raster layer using a path to an image.
        // Note: You can create a raster layer from a url, project item, or data connection.
        rasterLayer = (RasterLayer)LayerFactory.Instance.CreateLayer(new Uri(url), aMap);
      });
      #endregion

      #region Update the raster colorizer on a raster layer
      await QueuedTask.Run(() =>
      {
        // Get the colorizer from the raster layer.
        CIMRasterColorizer rasterColorizer = rasterLayer.GetColorizer();
        // Update raster colorizer properties.
        rasterColorizer.Brightness = 10;
        rasterColorizer.Contrast = -5;
        rasterColorizer.ResamplingType = RasterResamplingType.NearestNeighbor;
        // Update the raster layer with the changed colorizer.
        rasterLayer.SetColorizer(rasterColorizer);
      });
      #endregion

      #region Update the RGB colorizer on a raster layer
      await QueuedTask.Run(() =>
      {
        // Get the colorizer from the raster layer.
        CIMRasterColorizer rColorizer = rasterLayer.GetColorizer();
        // Check if the colorizer is an RGB colorizer.
        if (rColorizer is CIMRasterRGBColorizer)
        {
          CIMRasterRGBColorizer rasterRGBColorizer = (CIMRasterRGBColorizer)rColorizer;
          // Update RGB colorizer properties.
          rasterRGBColorizer.StretchType = RasterStretchType.ESRI;
          // Update the raster layer with the changed colorizer.
          rasterLayer.SetColorizer((CIMRasterColorizer)rasterRGBColorizer);
        }
      });
      #endregion

      #region Check if a certain colorizer can be applied to a raster layer 
      await QueuedTask.Run(() =>
      {
        // Get the list of colorizers that can be applied to the raster layer.
        IEnumerable<RasterColorizerType> applicableColorizerList = rasterLayer.GetApplicableColorizers();
        // Check if the RGB colorizer is part of the list.
        bool isTrue_ContainTheColorizerType =
          applicableColorizerList.Contains(RasterColorizerType.RGBColorizer);
      });
      #endregion

      #region Create a new colorizer based on a default colorizer definition and apply it to the raster layer 
      await QueuedTask.Run(async () =>
      {
        // Check if the Stretch colorizer can be applied to the raster layer.
        if (rasterLayer.GetApplicableColorizers().Contains(RasterColorizerType.StretchColorizer))
        {
          // Create a new Stretch Colorizer Definition using the default constructor.
          StretchColorizerDefinition stretchColorizerDef_default = new StretchColorizerDefinition();
          // Create a new Stretch colorizer using the colorizer definition created above.
          CIMRasterStretchColorizer newStretchColorizer_default =
            await rasterLayer.CreateColorizerAsync(stretchColorizerDef_default) as CIMRasterStretchColorizer;
          // Set the new colorizer on the raster layer.
          rasterLayer.SetColorizer(newStretchColorizer_default);
        }
      });
      #endregion

      #region Create a new colorizer based on a custom colorizer definition and apply it to the raster layer 
      await QueuedTask.Run(async () =>
      {
        // Check if the Stretch colorizer can be applied to the raster layer.
        if (rasterLayer.GetApplicableColorizers().Contains(RasterColorizerType.StretchColorizer))
        {
          // Create a new Stretch Colorizer Definition specifying parameters 
          // for band index, stretch type, gamma and color ramp.
          StretchColorizerDefinition stretchColorizerDef_custom =
            new StretchColorizerDefinition(1, RasterStretchType.ESRI, 2, colorRamp);
          // Create a new stretch colorizer using the colorizer definition created above.
          CIMRasterStretchColorizer newStretchColorizer_custom =
            await rasterLayer.CreateColorizerAsync(stretchColorizerDef_custom) as CIMRasterStretchColorizer;
          // Set the new colorizer on the raster layer.
          rasterLayer.SetColorizer(newStretchColorizer_custom);
        }
      });
      #endregion

      #region Create a raster layer with a new colorizer definition
      // Create a new stretch colorizer definition using default constructor.
      StretchColorizerDefinition stretchColorizerDef = new StretchColorizerDefinition();
      await QueuedTask.Run(() =>
      {
        // Create a raster layer using the colorizer definition created above.
        // Note: You can create a raster layer from a url, project item, or data connection.
        RasterLayer rasterLayerfromURL =
          LayerFactory.Instance.CreateRasterLayer(new Uri(url), aMap, 0, layerName, stretchColorizerDef) as RasterLayer;
      });
      #endregion
    }
    #region ProSnippet Group: Mosaic Layers
    #endregion
    public async Task MosaicLayers()
    {
      Map aMap = MapView.Active.Map;
      string layerName = null;
      CIMColorRamp colorRamp = null;

      #region Create a mosaic layer
      MosaicLayer mosaicLayer = null;
      string url = @"C:\Images\countries.gdb\Italy";
      await QueuedTask.Run(() =>
      {
        // Create a mosaic layer using a path to a mosaic dataset.
        // Note: You can create a mosaic layer from a url, project item, or data connection.
        mosaicLayer = (MosaicLayer)LayerFactory.Instance.CreateLayer(new Uri(url), aMap);
      });
      #endregion

      #region Update the raster colorizer on a mosaic layer
      await QueuedTask.Run(() =>
      {
        // Get the image sub-layer from the mosaic layer.
        ImageMosaicSubLayer mosaicImageSubLayer = mosaicLayer.GetImageLayer();
        // Get the colorizer from the image sub-layer.
        CIMRasterColorizer rasterColorizer = mosaicImageSubLayer.GetColorizer();
        // Update raster colorizer properties.
        rasterColorizer.Brightness = 10;
        rasterColorizer.Contrast = -5;
        rasterColorizer.ResamplingType = RasterResamplingType.NearestNeighbor;
        // Update the image sub-layer with the changed colorizer.
        mosaicImageSubLayer.SetColorizer(rasterColorizer);
      });
      #endregion

      #region Update the RGB colorizer on a mosaic layer
      await QueuedTask.Run(() =>
      {
        // Get the image sub-layer from the mosaic layer.
        ImageMosaicSubLayer mosaicImageSubLayer = mosaicLayer.GetImageLayer();
        // Get the colorizer from the image sub-layer.
        CIMRasterColorizer rColorizer = mosaicImageSubLayer.GetColorizer();
        // Check if the colorizer is an RGB colorizer.
        if (rColorizer is CIMRasterRGBColorizer)
        {
          // Cast colorizer type from CIMRasterColorizer into CIMRasterRGBColorizer.
          CIMRasterRGBColorizer rasterRGBColorizer = (CIMRasterRGBColorizer)rColorizer;
          // Update RGB colorizer properties.
          rasterRGBColorizer.StretchType = RasterStretchType.ESRI;
          // Update the image sub-layer with the changed colorizer.
          mosaicImageSubLayer.SetColorizer((CIMRasterColorizer)rasterRGBColorizer);
        }
      });
      #endregion

      #region Check if a certain colorizer can be applied to a mosaic layer 
      await QueuedTask.Run(() =>
      {
        // Get the image sub-layer from the mosaic layer.
        ImageMosaicSubLayer mosaicImageSubLayer = mosaicLayer.GetImageLayer();
        // Get the list of colorizers that can be applied to the image sub-layer.
        IEnumerable<RasterColorizerType> applicableColorizerList =
          mosaicImageSubLayer.GetApplicableColorizers();
        // Check if the RGB colorizer is part of the list.
        bool isTrue_ContainTheColorizerType =
          applicableColorizerList.Contains(RasterColorizerType.RGBColorizer);
      });
      #endregion

      #region Create a new colorizer based on a default colorizer definition and apply it to the mosaic layer 
      await QueuedTask.Run(async () =>
      {
        // Get the image sub-layer from the mosaic layer.
        ImageMosaicSubLayer mosaicImageSubLayer = mosaicLayer.GetImageLayer();
        // Check if the Stretch colorizer can be applied to the image sub-layer.
        if (mosaicImageSubLayer.GetApplicableColorizers().Contains(RasterColorizerType.StretchColorizer))
        {
          // Create a new Stretch Colorizer Definition using the default constructor.
          StretchColorizerDefinition stretchColorizerDef_default = new StretchColorizerDefinition();
          // Create a new Stretch colorizer using the colorizer definition created above.
          CIMRasterStretchColorizer newStretchColorizer_default =
            await mosaicImageSubLayer.CreateColorizerAsync(stretchColorizerDef_default) as CIMRasterStretchColorizer;
          // Set the new colorizer on the image sub-layer.
          mosaicImageSubLayer.SetColorizer(newStretchColorizer_default);
        }
      });
      #endregion

      #region Create a new colorizer based on a custom colorizer definition and apply it to the mosaic layer 
      await QueuedTask.Run(async () =>
      {
        // Get the image sub-layer from the mosaic layer.
        ImageMosaicSubLayer mosaicImageSubLayer = mosaicLayer.GetImageLayer();
        // Check if the Stretch colorizer can be applied to the image sub-layer.
        if (mosaicImageSubLayer.GetApplicableColorizers().Contains(RasterColorizerType.StretchColorizer))
        {
          // Create a new Stretch colorizer definition specifying parameters
          // for band index, stretch type, gamma and color ramp.
          StretchColorizerDefinition stretchColorizerDef_custom =
            new StretchColorizerDefinition(1, RasterStretchType.ESRI, 2, colorRamp);
          // Create a new stretch colorizer using the colorizer definition created above.
          CIMRasterStretchColorizer newStretchColorizer_custom =
            await mosaicImageSubLayer.CreateColorizerAsync(stretchColorizerDef_custom) as CIMRasterStretchColorizer;
          // Set the new colorizer on the image sub-layer.
          mosaicImageSubLayer.SetColorizer(newStretchColorizer_custom);
        }
      });
      #endregion

      #region Create a mosaic layer with a new colorizer definition
      // Create a new colorizer definition using default constructor.
      StretchColorizerDefinition stretchColorizerDef = new StretchColorizerDefinition();
      await QueuedTask.Run(() =>
      {
        // Create a mosaic layer using the colorizer definition created above.
        // Note: You can create a mosaic layer from a url, project item, or data connection.
        MosaicLayer newMosaicLayer =
          LayerFactory.Instance.CreateMosaicLayer(new Uri(url), aMap, 0, layerName, stretchColorizerDef) as MosaicLayer;
      });
      #endregion

      #region Update the sort order - mosaic method on a mosaic layer
      await QueuedTask.Run(() =>
      {
        // Get the image sub-layer from the mosaic layer.
        ImageServiceLayer mosaicImageSubLayer = (ImageServiceLayer)mosaicLayer.GetImageLayer();
        // Get the mosaic rule.
        CIMMosaicRule mosaicingRule = mosaicImageSubLayer.GetMosaicRule();
        // Set the Mosaic Method to Center.
        mosaicingRule.MosaicMethod = RasterMosaicMethod.Center;
        // Update the mosaic with the changed mosaic rule.
        mosaicImageSubLayer.SetMosaicRule(mosaicingRule);
      });
      #endregion

      #region Update the resolve overlap - mosaic operator on a mosaic layer
      await QueuedTask.Run(() =>
      {
        // Get the image sub-layer from the mosaic layer.
        ImageServiceLayer mosaicImageSublayer = (ImageServiceLayer)mosaicLayer.GetImageLayer();
        // Get the mosaic rule.
        CIMMosaicRule mosaicRule = mosaicImageSublayer.GetMosaicRule();
        // Set the Mosaic Operator to Mean.
        mosaicRule.MosaicOperatorType = RasterMosaicOperatorType.Mean;
        // Update the mosaic with the changed mosaic rule.
        mosaicImageSublayer.SetMosaicRule(mosaicRule);
      });
      #endregion
    }
    #region ProSnippet Group: Image Service Layers
    #endregion
    public async Task ImageServiceLayers()
    {
      Map aMap = MapView.Active.Map;
      string layerName = null;
      CIMColorRamp colorRamp = null;

      #region Create an image service layer
      ImageServiceLayer isLayer = null;
      string url =
      @"http://imagery.arcgisonline.com/arcgis/services/LandsatGLS/GLS2010_Enhanced/ImageServer";
      await QueuedTask.Run(() =>
      {
        // Create an image service layer using the url for an image service.
        isLayer = (ImageServiceLayer)LayerFactory.Instance.CreateLayer(new Uri(url), aMap);
      });
      #endregion

      #region Update the raster colorizer on an image service layer
      await QueuedTask.Run(() =>
      {
        // Get the colorizer from the image service layer.
        CIMRasterColorizer rasterColorizer = isLayer.GetColorizer();
        // Update the colorizer properties.
        rasterColorizer.Brightness = 10;
        rasterColorizer.Contrast = -5;
        rasterColorizer.ResamplingType = RasterResamplingType.NearestNeighbor;
        // Update the image service layer with the changed colorizer.
        isLayer.SetColorizer(rasterColorizer);
      });
      #endregion

      #region Update the RGB colorizer on an image service layer
      await QueuedTask.Run(() =>
      {
        // Get the colorizer from the image service layer.
        CIMRasterColorizer rColorizer = isLayer.GetColorizer();
        // Check if the colorizer is an RGB colorizer.
        if (rColorizer is CIMRasterRGBColorizer)
        {
          // Cast colorizer type from CIMRasterColorizer to CIMRasterRGBColorizer.
          CIMRasterRGBColorizer rasterRGBColorizer = (CIMRasterRGBColorizer)rColorizer;
          // Update RGB colorizer properties.
          rasterRGBColorizer.StretchType = RasterStretchType.ESRI;
          // Update the image service layer with the changed colorizer.
          isLayer.SetColorizer((CIMRasterColorizer)rasterRGBColorizer);
        }
      });
      #endregion

      #region Check if a certain colorizer can be applied to an image service layer 
      await QueuedTask.Run(() =>
      {
        // Get the list of colorizers that can be applied to the imager service layer.
        IEnumerable<RasterColorizerType> applicableColorizerList = isLayer.GetApplicableColorizers();
        // Check if the RGB colorizer is part of the list.
        bool isTrue_ContainTheColorizerType =
          applicableColorizerList.Contains(RasterColorizerType.RGBColorizer);
      });
      #endregion

      #region Create a new colorizer based on a default colorizer definition and apply it to the image service layer 
      await QueuedTask.Run(async () =>
      {
        // Check if the Stretch colorizer can be applied to the image service layer.
        if (isLayer.GetApplicableColorizers().Contains(RasterColorizerType.StretchColorizer))
        {
          // Create a new Stretch Colorizer Definition using the default constructor.
          StretchColorizerDefinition stretchColorizerDef_default = new StretchColorizerDefinition();
          // Create a new Stretch colorizer using the colorizer definition created above.
          CIMRasterStretchColorizer newStretchColorizer_default =
            await isLayer.CreateColorizerAsync(stretchColorizerDef_default) as CIMRasterStretchColorizer;
          // Set the new colorizer on the image service layer.
          isLayer.SetColorizer(newStretchColorizer_default);
        }
      });
      #endregion

      #region Create a new colorizer based on a custom colorizer definition and apply it to the image service layer 
      await QueuedTask.Run(async () =>
      {
        // Check if the Stretch colorizer can be applied to the image service layer.
        if (isLayer.GetApplicableColorizers().Contains(RasterColorizerType.StretchColorizer))
        {
          // Create a new Stretch Colorizer Definition specifying parameters
          // for band index, stretch type, gamma and color ramp. 
          StretchColorizerDefinition stretchColorizerDef_custom =
            new StretchColorizerDefinition(1, RasterStretchType.ESRI, 2, colorRamp);
          // Create a new stretch colorizer using the colorizer definition created above.
          CIMRasterStretchColorizer newStretchColorizer_custom =
            await isLayer.CreateColorizerAsync(stretchColorizerDef_custom) as CIMRasterStretchColorizer;
          // Set the new colorizer on the image service layer.
          isLayer.SetColorizer(newStretchColorizer_custom);
        }
      });
      #endregion

      #region Create an image service layer with a new colorizer definition
      // Create a new colorizer definition using default constructor.
      StretchColorizerDefinition stretchColorizerDef = new StretchColorizerDefinition();
      await QueuedTask.Run(() =>
      {
        // Create an image service layer using the colorizer definition created above.
        ImageServiceLayer imageServiceLayer =
          LayerFactory.Instance.CreateRasterLayer(new Uri(url), aMap, 0, layerName, stretchColorizerDef) as ImageServiceLayer;
      });
      #endregion

      #region Update the sort order - mosaic method on an image service layer
      await QueuedTask.Run(() =>
      {
        // Get the mosaic rule of the image service.
        CIMMosaicRule mosaicRule = isLayer.GetMosaicRule();
        // Set the Mosaic Method to Center.
        mosaicRule.MosaicMethod = RasterMosaicMethod.Center;
        // Update the image service with the changed mosaic rule.
        isLayer.SetMosaicRule(mosaicRule);
      });
      #endregion

      #region Update the resolve overlap - mosaic operator on an image service layer
      await QueuedTask.Run(() =>
      {
        // Get the mosaic rule of the image service.
        CIMMosaicRule mosaicingRule = isLayer.GetMosaicRule();
        // Set the Mosaic Operator to Mean.
        mosaicingRule.MosaicOperatorType = RasterMosaicOperatorType.Mean;
        // Update the image service with the changed mosaic rule.
        isLayer.SetMosaicRule(mosaicingRule);
      });
      #endregion

    }


    #region ProSnippet Group: Device Location API, GPS/GNSS Devices
    #endregion

    public async Task GNSS()
    {
      {
        #region Connect to a Device Location Source

        var newSrc = new SerialPortDeviceLocationSource();

        //Specify the COM port the device is connected to
        newSrc.ComPort = "Com3";
        newSrc.BaudRate = 4800;
        newSrc.AntennaHeight = 3;  // meters
        //fill in other properties as needed

        var props = new DeviceLocationProperties();
        props.AccuracyThreshold = 10;   // meters

        // jump to the background thread
        await QueuedTask.Run(() =>
        {
          //open the device
          DeviceLocationService.Instance.Open(newSrc, props);
        });

        #endregion
      }

      {

        #region Get the Current Device Location Source

        var source = DeviceLocationService.Instance.GetSource();
        if (source == null)
				{
          //There is no current source
				}

        #endregion

        #region Close the Current Device Location Source
        //Is there a current device source?
        var src = DeviceLocationService.Instance.GetSource();
        if (src == null)
          return;//no current source

        await QueuedTask.Run(() =>
        {
          DeviceLocationService.Instance.Close();
        });

        #endregion
      }

      {
        #region Get Current Device Location Source and Properties

        bool isConnected = DeviceLocationService.Instance.IsDeviceConnected();

        var src = DeviceLocationService.Instance.GetSource();

        if (src is SerialPortDeviceLocationSource serialPortSrc)
        {
          var port = serialPortSrc.ComPort;
          var antennaHeight = serialPortSrc.AntennaHeight;
          var dataBits = serialPortSrc.DataBits;
          var baudRate = serialPortSrc.BaudRate;
          var parity = serialPortSrc.Parity;
          var stopBits = serialPortSrc.StopBits;

          // retrieving spatial reference needs the MCT
          var sr = await QueuedTask.Run(() =>
          {
            return serialPortSrc.GetSpatialReference();
          });

        }
        var dlProps = DeviceLocationService.Instance.GetProperties();
        var accuracy = dlProps.AccuracyThreshold;
        
        #endregion
      }

      {

        #region Update Properties on the Current Device Location Source

        await QueuedTask.Run(() =>
        {
          var dlProps = DeviceLocationService.Instance.GetProperties();
          //Change the accuracy threshold
          dlProps.AccuracyThreshold = 22.5; // meters

          DeviceLocationService.Instance.UpdateProperties(dlProps);
        });
        #endregion
      }

      #region ProSnippet Group: Map Device Location Options
      #endregion

      {
        #region Enable/Disable Current Device Location Source For the Map

        bool enabled = MapDeviceLocationService.Instance.IsDeviceLocationEnabled;

        await QueuedTask.Run(() =>
        {
          MapDeviceLocationService.Instance.SetDeviceLocationEnabled(!enabled);
        });

        #endregion
      }

      {
        #region Get Current Map Device Location Options

        var options = MapDeviceLocationService.Instance.GetDeviceLocationOptions();

        var visibility = options.DeviceLocationVisibility;
        var navMode = options.NavigationMode;
        var trackUp = options.TrackUpNavigation;

        #endregion
      }

			{
        #region Check if The Current Device Location Is Enabled On The Map

        if (MapDeviceLocationService.Instance.IsDeviceLocationEnabled)
				{
          //The Device Location Source is Enabled
				}

        #endregion
      }
			{

        #region Set Current Map Device Location Options

        //Must be on the QueuedTask.Run()

        //Check there is a source first...
        if (DeviceLocationService.Instance.GetSource() == null)
          //Setting DeviceLocationOptions w/ no Device Location Source
          //Will throw an InvalidOperationException
          return;

        var map = MapView.Active.Map;
        if (!MapDeviceLocationService.Instance.IsDeviceLocationEnabled)
          //Setting DeviceLocationOptions w/ no Device Location Enabled
          //Will throw an InvalidOperationException
          return;

        MapDeviceLocationService.Instance.SetDeviceLocationOptions(
          new MapDeviceLocationOptions()
          {
            DeviceLocationVisibility = true,
            NavigationMode = MappingDeviceLocationNavigationMode.KeepAtCenter,
            TrackUpNavigation = true
          });
        
        #endregion
      }
      {
        #region Zoom/Pan The Map To The Most Recent Location

        //Must be on the QueuedTask.Run()

        if (!MapDeviceLocationService.Instance.IsDeviceLocationEnabled)
          //Calling ZoomOrPanToCurrentLocation w/ no Device Location Enabled
          //Will throw an InvalidOperationException
          return;

        // true for zoom, false for pan
        MapDeviceLocationService.Instance.ZoomOrPanToCurrentLocation(true);

        #endregion
      }

      {
        GraphicsLayer graphicsLayer = null;

        #region Add the Most Recent Location To A Graphics Layer

        //var graphicsLayer = ... ;
        //Must be on the QueuedTask.Run()

        // get the last location
        var pt = DeviceLocationService.Instance.GetCurrentSnapshot()?.GetPositionAsMapPoint();
        if (pt != null)
				{
          //Create a point symbol
          var ptSymbol = SymbolFactory.Instance.ConstructPointSymbol(
                            CIMColor.CreateRGBColor(125, 125, 0), 10, SimpleMarkerStyle.Triangle);
          //Add a graphic to the graphics layer
          graphicsLayer.AddElement(pt, ptSymbol);
          //unselect it
          graphicsLayer.ClearSelection();
        }

        #endregion
      }

    }

    #region Subscribe to Location Snapshot event
    private void SubscribeToEvents()
    {
      SnapshotChangedEvent.Subscribe(OnSnapshotChanged);
    }
    private void OnSnapshotChanged(SnapshotChangedEventArgs args)
    {
      if (args == null)
        return;

      var snapshot = args.Snapshot as NMEASnapshot;
      if (snapshot == null)
        return;

      QueuedTask.Run(() =>
      {
        var pt = snapshot.GetPositionAsMapPoint();
        if (pt?.IsEmpty ?? true)
          return;

        // access properties
        var alt = snapshot.Altitude;
        var dt = snapshot.DateTime;
        var vdop = snapshot.VDOP;
        var hdop = snapshot.HDOP;
        // etc

        //TODO: use the snapshot
      });
    }
    #endregion

    #region ProSnippet Group: Feature Masking
    #endregion

    public void Masking1()
    {
      #region Get the Mask Geometry for a Feature

      var featureLayer = MapView.Active.Map.GetLayersAsFlattenedList()
                                 .OfType<BasicFeatureLayer>().FirstOrDefault();
      if (featureLayer == null)
        return;

      var mv = MapView.Active;

      QueuedTask.Run(() =>
      {
        //get the first feature...
        //...assuming at least one feature gets selected
        var rc = featureLayer.GetTable().Search();
        rc.MoveNext();
        var oid = rc.Current.GetObjectID();

        //Use DrawingOutlineType.BoundingEnvelope to retrieve a generalized
        //mask geometry or "Box". The mask will be in the same SpatRef as the map
        var mask_geom = featureLayer.QueryDrawingOutline(oid, mv, DrawingOutlineType.Exact);

        //TODO - use the mask geometry...
        rc.Dispose();
      });
      #endregion

    }
  }

}
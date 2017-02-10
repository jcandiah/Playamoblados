using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeYourMarket.Model.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Plus.v1;
using Google.Apis.Plus.v1.Data;
using System.Web.Hosting;
using Google.Apis.Util.Store;

namespace BeYourMarket.Web.Utilities
{
    public static class SpreadsheetHelper
    {
        public static void WriteOtCert(Order order, Listing listing, AspNetUser pasajero)
        {
            var httpContext = Elmah.ErrorSignal.FromCurrentContext();

            //Task.Factory.StartNew(() =>
            //{
            try
            {
                var scopes = new[] { SheetsService.Scope.Spreadsheets };
                //var user = "565807602518-se94vecnfk43sjqqli6pv3g3kvfjcvoe.apps.googleusercontent.com";
                var user = "565807602518-compute@developer.gserviceaccount.com";
                var certificate = new X509Certificate2(HostingEnvironment.MapPath("~/Playamoblados-5ab608a77296.p12"), "notasecret", X509KeyStorageFlags.Exportable);
                var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(user) { Scopes = scopes }.FromCertificate(certificate));
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Sheets API Playamoblados"
                });

                var spreadsheetId = "14c2Klhu-p07RLCQAJgQ4OVn5gEKJZHikourDtUe4BYM";
                var range = "Reservas!A1";

                var body = new ValueRange
                {
                    Values = new List<IList<object>>()
                        {
                            new List<object>()
                            {
                                order.OT, // Orden de trabajo
								order.ID, // ID reserva
								order.ListingID, // ID propiedad
								order.FromDate.Value.ToShortDateString(), // Fecha desde
								order.ToDate.Value.ToShortDateString(), // Fecha hasta
								order.Quantity, // Cantidad de noches
								listing.Price, // Valor por noche (por defecto)
								order.Total, // Valor total
								order.Percent, // % de abono
								order.Abono, // Valor abono
								listing.CleanlinessPrice, // Valor aseo
								pasajero.FullName, // Nombre pasajero
								order.Adults + order.Children, // Cantidad de pasajeros
								pasajero.PhoneNumber, // Teléfono pasajero
								pasajero.Email // Correo electrónico pasajero
							},
                        }
                };

                var request = service.Spreadsheets.Values.Append(body, spreadsheetId, range);
                request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                request.IncludeValuesInResponse = false;
                request.ResponseValueRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseValueRenderOptionEnum.FORMATTEDVALUE;
                request.ResponseDateTimeRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseDateTimeRenderOptionEnum.FORMATTEDSTRING;
                request.Execute();
            }
            catch (Exception ex)
            {
                //http://stackoverflow.com/questions/7441062/how-to-use-elmah-to-manually-log-errors
                httpContext.Raise(ex);
            }
            //});
        }

        public static void WriteOtOAuth(Order order, Listing listing, AspNetUser pasajero)
        {
            var httpContext = Elmah.ErrorSignal.FromCurrentContext();

            //Task.Factory.StartNew(() =>
            //{
            try
            {
                var path = HostingEnvironment.MapPath("~/GData");
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }

                var secrets = new ClientSecrets
                {
                    ClientId = "565807602518-se94vecnfk43sjqqli6pv3g3kvfjcvoe.apps.googleusercontent.com",
                    ClientSecret = "7HmtNlgx8N6R-5Adlb-cO4t2"
                };
                var scopes = new[] { SheetsService.Scope.Spreadsheets };
                //var user = "playamoblados@ratio.cl";
                var user = "user";
                var dataStore = new FileDataStore(path, true);
                var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, scopes, secrets.ClientId, CancellationToken.None, dataStore).Result;
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Sheets API Playamoblados"
                });

                var spreadsheetId = "14c2Klhu-p07RLCQAJgQ4OVn5gEKJZHikourDtUe4BYM";
                var range = "Reservas!A1";

                var body = new ValueRange
                {
                    Values = new List<IList<object>>()
                        {
                            new List<object>()
                            {
                                order.OT, // Orden de trabajo
								order.ID, // ID reserva
								order.ListingID, // ID propiedad
								order.FromDate.Value.ToShortDateString(), // Fecha desde
								order.ToDate.Value.ToShortDateString(), // Fecha hasta
								order.Quantity, // Cantidad de noches
								listing.Price, // Valor por noche (por defecto)
								order.Total, // Valor total
								order.Percent, // % de abono
								order.Abono, // Valor abono
								listing.CleanlinessPrice, // Valor aseo
								pasajero.FullName, // Nombre pasajero
								order.Adults + order.Children, // Cantidad de pasajeros
								pasajero.PhoneNumber, // Teléfono pasajero
								pasajero.Email // Correo electrónico pasajero
							},
                        }
                };

                var request = service.Spreadsheets.Values.Append(body, spreadsheetId, range);
                request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                request.IncludeValuesInResponse = false;
                request.ResponseValueRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseValueRenderOptionEnum.FORMATTEDVALUE;
                request.ResponseDateTimeRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseDateTimeRenderOptionEnum.FORMATTEDSTRING;
                request.Execute();
            }
            catch (Exception ex)
            {
                //http://stackoverflow.com/questions/7441062/how-to-use-elmah-to-manually-log-errors
                httpContext.Raise(ex);
            }
            //});
        }
    }
}
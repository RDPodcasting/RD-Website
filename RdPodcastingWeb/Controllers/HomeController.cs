﻿using CsvHelper;
using DocumentFormat.OpenXml.ExtendedProperties;
using Newtonsoft.Json;
using RdPodcasting.Domain;
using RdPodcasting.Domain.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace RdPodcastingWeb.Controllers
{
 
    public class HomeController : Controller
    {
        DAO dao;

        [OutputCache(CacheProfile = "CacheLong")]
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Sobre()
        {
            return View();
        }
        public ActionResult Episodios()
        {
            return View();
        }
        public ActionResult About()
        {
            return View();
        }
        public ActionResult Contato()
        {
            return View();
        }

        [Route("{dataBusca:string}")]
        public async Task<ActionResult> Covid19(string dataBusca)
        {
            dao = new DAO(Server.MapPath("~/files/"));
            string _dia = null;
            string _mes = null;
            string _ano = null;

            if (dataBusca != "start")
            {
                _dia = dataBusca.Split('-')[0];
                _mes = dataBusca.Split('-')[1];
                _ano = dataBusca.Split('-')[2];
            }
            else
            {
                _dia = dao.AdicionarZero((DateTime.Now.Day).ToString());
                _mes = dao.AdicionarZero(DateTime.Now.Month.ToString());
                _ano = DateTime.Now.Year.ToString();
            }
            DataCovid19 covidData;
            int subtrair = 0;
            int diaLoop = 0;
            bool mesanterior = false;
            do
            {
                if (!mesanterior)
                    diaLoop = string.IsNullOrEmpty(_dia) ? 0 : int.Parse(_dia) - subtrair;

                covidData = Task.Run(() => dao.ConstruirObjetoDeHoje(dao.AdicionarZero(diaLoop.ToString()), _mes, _ano)).Result;
                subtrair += 1;

                if (diaLoop == 0)
                {
                    _mes = dao.AdicionarZero((int.Parse(_mes) - 1).ToString());
                    diaLoop = DateTime.DaysInMonth(int.Parse(_ano), int.Parse(_mes));
                    mesanterior = true;
                }
                else if (covidData.Last_Update != null)
                    break;
            } while (covidData.Last_Update == null);


            covidData.Last_Update = Convert.ToDateTime(covidData.Last_Update).ToString("D",
            CultureInfo.CreateSpecificCulture("pt-BR"));
                      
            ViewBag.CovidData = covidData;

            var casosDoMesAtual = await dao.CasosDoMesAtual();
            casosDoMesAtual.CasosConfirmados = Convert.ToDouble(covidData.Confirmed);
            ViewBag.CovidMesAtual = casosDoMesAtual;
            int mesAnterior = DateTime.Now.Month - 1;

            var casosDoMes = await dao.CasosDoMes(mesAnterior, DateTime.Now.Year);

            ViewBag.CovidMesAnterior = casosDoMes;
            return View();
        }
  
        public async Task<JsonResult> GetDataGraficoDias()
        {
            dao = new DAO(Server.MapPath("~/files/"));
            var lista = await dao.ConstruirListaParaGraficos();
            return Json(lista,JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetDataGraficoCasosEstados()
        {
            List<ResultLine> listaEstadosFull = await CarregarPorHttp();
            return Json(listaEstadosFull, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetDataGraficoMortosEstados()
        {
            List<ResultLine> listaEstadosFull = await CarregarPorHttp();

            return Json(listaEstadosFull, JsonRequestBehavior.AllowGet);
        }
      
        public async Task<List<ResultLine>> CarregarPorHttp()
        {
            dao = new DAO(Server.MapPath("~/files/"));

            string _dia = dao.AdicionarZero((DateTime.Now.Day).ToString());
            string _mes = dao.AdicionarZero(DateTime.Now.Month.ToString());
            string _ano = DateTime.Now.Year.ToString();

            List<ResultLine> listaEstadosFull = CarregarPorArquivo(_dia, _mes, _ano);

            if (listaEstadosFull != null)
                return listaEstadosFull;

            var client = new HttpClient();
            string queryString = "https://brasil.io/api/dataset/covid19/caso/data/";
            
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(queryString);
                if (response.IsSuccessStatusCode)
                {
                    var resultado = response.Content.ReadAsStringAsync().Result;
                    EstadosCovid dataJ = JsonConvert.DeserializeObject<EstadosCovid>(resultado.ToString());

                    listaEstadosFull = dataJ.results
                    .GroupBy(l => l.state)
                    .Select(cl => new ResultLine
                    {
                        Estado = cl.First().state,
                        Casos = cl.Sum(a => a.confirmed),
                        Mortos = cl.Sum(b => b.deaths).ToString(),
                    }).ToList();

                    var json = JsonConvert.SerializeObject(listaEstadosFull);
                    var caminho = Server.MapPath("~/files/") + _mes + "-" + _dia + "-" + _ano + ".json";
                    System.IO.File.WriteAllText(caminho, json);

                }

            }
            catch (Exception ex)
            {

            }

            return listaEstadosFull;
        }

        public List<ResultLine> CarregarPorArquivo(string dia, string mes, string ano)
        {
            string caminho = Server.MapPath("~/files/");
            string fileName =  caminho + mes + "-" + dia + "-" + ano + ".json";
            string json;
            List<ResultLine> lista = null;
            if (System.IO.File.Exists(fileName))
            {
                json = System.IO.File.ReadAllText(fileName);

                lista = JsonConvert.DeserializeObject<List<ResultLine>>(json);
            }

            return lista;
        }

    }
}
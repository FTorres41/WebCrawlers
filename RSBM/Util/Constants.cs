﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Util
{
    class Constants
    {
        /*CONSTANTES BEC*/
        internal static string BEC_SITE = "www.bec.sp.gov.br";
        internal static string BEC_UF = "SP";
        internal static string BEC_CIDADE = "SAO PAULO";
        internal static string BEC_ESTADO = "SAO PAULO";
        internal static string BEC_LINK_MODALIDADE_72 = "https://www.bec.sp.gov.br/becsp/aspx/NaturezaDespesa.aspx?chave=&modalidade=72";
        internal static string BEC_LINK_MODALIDADE_71 = "https://www.bec.sp.gov.br/becsp/aspx/NaturezaDespesa.aspx?chave=&modalidade=71";
        internal static string BEC_LINK_MODALIDADE_2 = "https://www.bec.sp.gov.br/becsp/aspx/NaturezaDespesa.aspx?chave=&modalidade=2";
        //Link diferente para a modalidade 5 pois pelo link de modalidade acima as licitações estão todas quebradas
        internal static string BEC_LINK_MODALIDADE_5 = "https://www.bec.sp.gov.br/BEC_Dispensa_UI/ui/BEC_DL_Pesquisa.aspx?chave=";
        internal static string BEC_LINK_OC = "https://www.bec.sp.gov.br/Publico/aspx/dadosOC.aspx?nroOC=";
        internal static string BEC_ID_NATUREZA = "ctl00_ContentPlaceHolder1_WUC_NaturezaDespesaResumo1_gvNaturezaDespesa_";
        internal static string BEC_ID_NATUREZA_RESUMO = "ctl00_ContentPlaceHolder1_gvResumoNatureza";

        /*CONSTANTES CMG*/
        internal static string CMG_HOST = "www1.compras.mg.gov.br";
        internal static string CMG_SITE = "https://www1.compras.mg.gov.br/";
        internal static string CMG_UF = "MG";
        internal static string CMG_CIDADE = "BELO HORIZONTE";
        internal static string CMG_ESTADO = "MINAS GERAIS";
        internal static string CMG_CAPTCHA = "https://www1.compras.mg.gov.br/kaptcha.jpg";
        internal static string CMG_LINK_PAGINATION = "processocompra/pregao/consulta/consultaPregoes.html";
        internal static string CMG_PARAMETERS_PAGINATION = "tabConsultaPregoes_paginaCorrente={0}&tabConsultaPregoes_ordenacao=1-1,2-1&objetoLicitacaoPlanejamentoOpcaoEOu=E&situacaoPregao=NAO_INICIADA&localEntregaItemOpcaoEOu=E&tipoPregao=PREGAO&metodo=pesquisar&descricaoMaterialServicoOpcaoEOu=E&objetoLicitacaoProcessoOpcaoEOu=E&especificacaoItemMaterialServicoOpcaoEOu=E&descricaoLoteOpcaoEOu=E&anoProcessoCompra={1}&textoConfirmacao=";
        internal static string CMG_LINK_EDITAL = "processocompra/pregao/consulta/dados/abaDadosPregao.html";

        /*CONSTANTES BB*/
        internal static string BB_HOST = "https://www.licitacoes-e.com.br";
        internal static string BB_SITE = "https://www.licitacoes-e.com.br/aop/pesquisar-licitacao.aop?opcao=preencherPesquisar";
        internal static string BB_LINK_EDITAL = "https://www.licitacoes-e.com.br/aop/documentos/L-{0}/{1}";
        internal static string BB_LINK_LICITACAO = "https://www.licitacoes-e.com.br/aop/consultar-detalhes-licitacao.aop?numeroLicitacao={0}&opcao=consultarDetalhesLicitacao";
        internal static string BB_WORKAROUND_LINK = "https://www.licitacoes-e.com.br/aop/pesquisar-licitacao.aop?opcao=preencherPesquisarSituacao&textoMercadoria=";
        internal static string BB_SITEKEY = "6LdZf6EUAAAAAA6rdcrc_sVCi91CqWS3EZcFYayF";
        internal static string BB_LINK_ANEXO = "https://www.licitacoes-e.com.br/aop/consultar-anexo-licitacao.aop?opcao=consultarEdital&numeroLicitacao={0}&numeroAnexoLicitacao={1}";

        /*CONSTANTES CN*/
        internal static string CN_HOST = "www.comprasnet.gov.br";
        internal static string CN_SITE = "http://www.comprasnet.gov.br/ConsultaLicitacoes/ConsLicitacao_Relacao.asp?numprp=&dt_publ_ini={0}&dt_publ_fim={1}&chkModalidade=1,2,3,20,5,99&chk_concor=31,32,41,42&chk_pregao=1,2,3,4&chk_rdc=1,2,3,4&optTpPesqMat=M&optTpPesqServ=S&chkTodos=-1&chk_concorTodos=-1&chk_pregaoTodos=-1&txtlstUf=&txtlstMunicipio=&txtlstUasg=&txtlstGrpMaterial=&txtlstClasMaterial=&txtlstMaterial=&txtlstGrpServico=&txtlstServico=&txtObjeto=&numpag={2}";
        internal static string CN_EDITAL_LINK = "http://www.comprasnet.gov.br/ConsultaLicitacoes/Download/Download.asp";
        internal static string CN_ITENS_EDITAL = "http://www.comprasnet.gov.br/ConsultaLicitacoes/download/download_editais_detalhe.asp{0}&tipo={1}";
        internal static string CN_HISTORICO_LINK = "http://www.comprasnet.gov.br/ConsultaLicitacoes/ConsLicitacao_HistoricoEventos.asp?";
        internal static string CN_ATA_PREGAO = "http://comprasnet.gov.br/livre/pregao/ata2.asp?co_no_uasg={0}&numprp={1}";
        internal static string CN_PREGAO_AVISOS_DETALHE = "http://comprasnet.gov.br/livre/pregao/avisos2.asp?prgCod={0}&Origem=Avisos&Tipo={1}";
        internal static string CN_PREGAO_AVISOS_ITEM = "http://comprasnet.gov.br/livre/Pregao/avisos4.asp?{0}";

        /* CNET COTAÇÕES */
        internal static string CN_COTACOES = "http://comprasnet.gov.br/cotacao/menu.asp?filtro=livre_andamento";
        internal static string CN_COTACAO_LINK = "http://comprasnet.gov.br/cotacao/{0}";

        /* CNET PREÇOS */
        internal static string CN_PAINEL_MATERIAIS = "http://paineldeprecos.planejamento.gov.br/analise-materiais";
        internal static string CN_PAINEL_SERVICOS = "http://paineldeprecos.planejamento.gov.br/analise-servicos";
        internal static string CNETPRECOS_NAME = "CNETPRECOS";
        internal static string CN_TERMOHOMOLOGACAO = "http://comprasnet.gov.br/livre/pregao/termohom.asp?prgcod={0}&co_no_uasg={1}&numprp={2}";

        /*CONSTANTES TCE PR*/
        internal static string TCEPR_SITE = "http://servicos.tce.pr.gov.br/TCEPR/Municipal/AML/ConsultarProcessoCompraWeb.aspx";
        internal static string TCEPR_HOST = "www.tce.pr.gov.br";
        internal static string TCEPR_CIDADE = "CURITIBA";
        internal static string TCEPR_UF = "PR";
        internal static string TCEPR_ESTADO = "PARANA";

        /*CONSTANTES TCM CE*/
        internal static string TCMCE_SITE = "https://licitacoes.tce.ce.gov.br/index.php/licitacao/abertas";
        internal static string TCMCE_PAGE = "https://licitacoes.tce.ce.gov.br/index.php/licitacao/abertas/page/{0}/ini/{1}/fim/{2}";
        internal static string TCMCE_CAPTCHA = "https://licitacoes.tce.ce.gov.br/captcha/captcha.php?dh={0}";
        internal static string TCMCE_POSTCAPTCHA = "https://licitacoes.tce.ce.gov.br/index.php/licitacao/verificaCaptcha";
        internal static string TCMCE_HOST = "https://licitacoes.tce.ce.gov.br";
        internal static string TCMCE_UF = "CE";
        internal static string TCMCE_ESTADO = "CEARÁ";
        internal static string TCMCE_CIDADE = "FORTALEZA";

        /*CONSTANTES TCE PI*/
        internal static string TCEPI_SITE = "https://sistemas.tce.pi.gov.br/licitacoesweb/mural/index.xhtml";
        internal static string TCEPI_PAGE = "https://sistemas.tce.pi.gov.br/licitacao/lcw_muralconcon.do?evento=portlet&pIdPlc=lcw_muralconconNav&acao=navega&pAcIniNavlcw_muralconconNav={0}&campo=";
        internal static string TCEPI_LICIT = "https://sistemas.tce.pi.gov.br/licitacoesweb/mural/detalhelicitacao.xhtml?id={0}";
        internal static string TCEPI_FILES = "https://sistemas.tce.pi.gov.br/licitacao/arquivolicitacaocon.do?evento=y&licitacaoWeb_id_Arg={0}";
        internal static string TCEPI_HOST = "www.tce.pi.gov.br";
        internal static string TCEPI_UF = "PI";
        internal static string TCEPI_ESTADO = "PIAUÍ";
        internal static string TCEPI_CIDADE = "TERESINA";

        /*CONSTANTES TCE SE*/
        internal static string TCESE_SITE = "https://www.tce.se.gov.br/portaldojurisdicionado/Prefeituras.aspx";
        internal static string TCESE_MUN_PAGE = "https://www.tce.se.gov.br/portaldojurisdicionado/ListaLicitacoesDaUnidade.aspx?cod={0}&pref={1}";
        internal static string TCESE_LICIT = "https://www.tce.se.gov.br/portaldojurisdicionado/DetalhesLicitacao.aspx?cod={0}&pref={1}";
        internal static List<string> TCESE_MUN_CODE = new List<string>() { "002306", "002312", "006310", "004313", "005306", "005302", "003301", "004311", "002305", "001301", "003307",
                                                                           "004301", "003310", "002309", "006308", "001306", "001309", "006311", "001305", "004304", "002302", "003312",
                                                                           "002310", "003302", "006313", "004310", "006306", "002303", "005304", "003308", "003305", "006303", "001312",
                                                                           "004312", "002313", "004309", "003313", "004308", "001303", "003306", "003304", "004305", "001304", "001307",
                                                                           "002304", "001313", "003303", "004303", "005301", "004302", "003309", "001302", "006301", "002301", "002308",
                                                                           "006304", "001311", "004306", "003311", "005303", "006312", "001310", "003328", "003314", "005305", "004314",
                                                                           "002311", "004307", "006302", "001308", "002307", "006305", "006307", "006309"};
        internal static List<string> TCESE_MUN_NAME = new List<string>() { "PREFEITURA MUNICIPAL DE AMPARO DO SAO FRANCISCO", "PREFEITURA MUNICIPAL DE AQUIDABA", "PREFEITURA MUNICIPAL DE ARAUA", "PREFEITURA MUNICIPAL DE AREIA BRANCA", "PREFEITURA MUNICIPAL DE BARRA DOS COQUEIROS", "PREFEITURA MUNICIPAL DE BOQUIM", "PREFEITURA MUNICIPAL DE BREJO GRANDE", "PREFEITURA MUNICIPAL DE CAMPO DO BRITO", "PREFEITURA MUNICIPAL DE CANHOBA", "PREFEITURA MUNICIPAL DE CANINDE DE SAO FRANCISCO", "PREFEITURA MUNICIPAL DE CAPELA",
                                                                           "PREFEITURA MUNICIPAL DE CARIRA", "PREFEITURA MUNICIPAL DE CARMOPOLIS", "PREFEITURA MUNICIPAL DE CEDRO DE SAO JOAO", "PREFEITURA MUNICIPAL DE CRISTINAPOLIS", "PREFEITURA MUNICIPAL DE CUMBE", "PREFEITURA MUNICIPAL DE DIVINA PASTORA", "PREFEITURA MUNICIPAL DE ESTANCIA", "PREFEITURA MUNICIPAL DE FEIRA NOVA", "PREFEITURA MUNICIPAL DE FREI PAULO", "PREFEITURA MUNICIPAL DE GARARU", "PREFEITURA MUNICIPAL DE GENERAL MAYNARD",
                                                                           "PREFEITURA MUNICIPAL DE GRACCHO CARDOSO", "PREFEITURA MUNICIPAL DE ILHA DAS FLORES", "PREFEITURA MUNICIPAL DE INDIAROBA", "PREFEITURA MUNICIPAL DE ITABAIANA", "PREFEITURA MUNICIPAL DE ITABAIANINHA", "PREFEITURA MUNICIPAL DE ITABI", "PREFEITURA MUNICIPAL DE ITAPORANGA D AJUDA", "PREFEITURA MUNICIPAL DE JAPARATUBA", "PREFEITURA MUNICIPAL DE JAPOATA", "PREFEITURA MUNICIPAL DE LAGARTO", "PREFEITURA MUNICIPAL DE LARANJEIRAS",
                                                                           "PREFEITURA MUNICIPAL DE MACAMBIRA", "PREFEITURA MUNICIPAL DE MALHADA DOS BOIS", "PREFEITURA MUNICIPAL DE MALHADOR", "PREFEITURA MUNICIPAL DE MARUIM", "PREFEITURA MUNICIPAL DE MOITA BONITA", "PREFEITURA MUNICIPAL DE MONTE ALEGRE", "PREFEITURA MUNICIPAL DE MURIBECA", "PREFEITURA MUNICIPAL DE NEOPOLIS", "PREFEITURA MUNICIPAL DE NOSSA SENHORA APARECIDA", "PREFEITURA MUNICIPAL DE NOSSA SENHORA DA GLORIA", "PREFEITURA MUNICIPAL DE NOSSA SENHORA DAS DORES",
                                                                           "PREFEITURA MUNICIPAL DE NOSSA SENHORA DE LOURDES", "PREFEITURA MUNICIPAL DE NOSSA SENHORA DO SOCORRO", "PREFEITURA MUNICIPAL DE PACATUBA", "PREFEITURA MUNICIPAL DE PEDRA MOLE", "PREFEITURA MUNICIPAL DE PEDRINHAS", "PREFEITURA MUNICIPAL DE PINHAO", "PREFEITURA MUNICIPAL DE PIRAMBU", "PREFEITURA MUNICIPAL DE POCO REDONDO", "PREFEITURA MUNICIPAL DE POCO VERDE", "PREFEITURA MUNICIPAL DE PORTO DA FOLHA", "PREFEITURA MUNICIPAL DE PROPRIA",
                                                                           "PREFEITURA MUNICIPAL DE RIACHAO DO DANTAS", "PREFEITURA MUNICIPAL DE RIACHUELO", "PREFEITURA MUNICIPAL DE RIBEIROPOLIS", "PREFEITURA MUNICIPAL DE ROSARIO DO CATETE", "PREFEITURA MUNICIPAL DE SALGADO", "PREFEITURA MUNICIPAL DE SANTA LUZIA DO ITANHY", "PREFEITURA MUNICIPAL DE SANTA ROSA DE LIMA", "PREFEITURA MUNICIPAL DE SANTANA DE SAO FRANCISCO", "PREFEITURA MUNICIPAL DE SANTO AMARO DAS BROTAS", "PREFEITURA MUNICIPAL DE SAO CRISTOVAO", "PREFEITURA MUNICIPAL DE SAO DOMINGOS",
                                                                           "PREFEITURA MUNICIPAL DE SAO FRANCISCO", "PREFEITURA MUNICIPAL DE SAO MIGUEL DO ALEIXO", "PREFEITURA MUNICIPAL DE SIMAO DIAS", "PREFEITURA MUNICIPAL DE SIRIRI", "PREFEITURA MUNICIPAL DE TELHA", "PREFEITURA MUNICIPAL DE TOBIAS BARRETO", "PREFEITURA MUNICIPAL DE TOMAR DO GERU", "PREFEITURA MUNICIPAL DE UMBAUBA"};
        internal static string TCESE_HOST = "www.tce.se.gov.br";
        internal static string TCESE_UF = "SE";
        internal static string TCESE_ESTADO = "SERGIPE";
        internal static string TCESE_CIDADE = "ARACAJU";

        /*CONSTANTES PCP - PORTAL COMPRAS PÚBLICAS*/
        internal static string PCP_NOME = "PCP";
        internal static string PCP_HOST = "http://portaldecompraspublicas.com.br/18/";
        internal static string PCP_PROCESSOS = "http://portaldecompraspublicas.com.br/18/Processos/";
        internal static string PCP_BUSCA = "http://portaldecompraspublicas.com.br/18/Processos/?rdCampoPesquisado=&ttPagina={0}&ttPublicacao={1}/{2}/{3}";
        internal static string PCP_PAGINA_BUSCA = "https://www.portaldecompraspublicas.com.br/18/Processos/?rdCampoPesquisado=&btBuscar=Buscar&ttBusca=&ttOrderBy=3&ttPagina={0}&ttObjeto=&ttOrgao=&ttAbertura=&ttPublicacao={1}/{2}/{3}&slTipo=&ttSeletorData=0&ttCaptcha={4}";
        internal static string PCP_LICIT = "http://portaldecompraspublicas.com.br/18/SessaoPublica/?ttCD_CHAVE={0}";
        internal static string PCP_SITEKEY = "6Le4ZSsUAAAAANQfiMHtwCiabg17Rk8_WNCFq5xt";
        internal static string PCP_DOCUMENTKEY = "6LdM4wUTAAAAAIkg2ZjFgO9miNPKJ0rFHZIXjuzU";
        internal static string PCP_FRAME = "https://www.google.com/recaptcha/api2/anchor?k=6LdM4wUTAAAAAIkg2ZjFgO9miNPKJ0rFHZIXjuzU&co=aHR0cDovL2F0YXNwLnBvcnRhbGRlY29tcHJhc3B1YmxpY2FzLmNvbS5icjo4MA..&hl=pt-BR&v=v1517812337239&size=normal&cb=wj2yqn5gyp3s";

        /*CONSTANTES TWOCAPTCHA*/
        //internal static string TWOCAPTCHA_APIKEY = "2ed54ed2d3277c0c3d43c947cd863797";
        internal static string TWOCAPTCHA_APIKEY = "b7b80f8ee63de025818e790b989e45d4";
        internal static string TWOCAPTCHA_POST = "http://2captcha.com/in.php?key={0}&method=userrecaptcha&googlekey={1}&pageurl={2}";
        internal static string TWOCAPTCHA_GET = "http://2captcha.com/res.php?key={0}&action=get&id={1}";

        /*CONSTANTES TCERS*/
        internal static string TCERS_NOME = "TCERS";
        internal static string TCERS_LINK = "http://www1.tce.rs.gov.br/portal/page/portal/tcers/";
        internal static string TCERS_HOST = "https://portal.tce.rs.gov.br/aplicprod/f?p=50500:1:::NO:::";
        internal static string TCERS_ESTADO = "RIO GRANDE DO SUL";
        internal static string TCERS_ESTADO_FONTE = "RS";
        internal static int TCERS_ID_FONTE = 1418;
        internal static string TCERS_SITEKEY = "6LfBjvkSAAAAAJyEpVkbE0LnNyfc2c03tVpZmQj9";
        internal static string TCERS_DOCUMENTSITEKEY = "6LfBjvkSAAAAAJyEpVkbE0LnNyfc2c03tVpZmQj9";
        internal static string TCERS_BASEURL = "https://portal.tce.rs.gov.br/aplicprod/";

        /*CONSTANTES COMPRAS RJ*/
        internal static string CRJ_NOME = "CRJ";
        //internal static string CRJ_HOST = "http://www.compras.rj.gov.br/publico/";
        internal static string CRJ_HOST = "https://www.compras.rj.gov.br/Portal-Siga/index";
        internal static string CRJ_LICITACOES_DO_DIA = "http://www.compras.rj.gov.br/publico/licitacoes_do_dia.asp";
        internal static string CRJ_LICITACOES_FUTURAS = "http://www.compras.rj.gov.br/publico/licitacoes_futuras.asp?offset={0}";
        internal static int CRJ_ID_FONTE = 1266;
        internal static string CRJ_ESTADO_FONTE = "RJ";
        internal static string CRJ_ESTADO = "RIO DE JANEIRO";
        internal static string CRJ_ARQUIVOS = "http://www.compras.rj.gov.br/publico/div_download_edital.asp?verificar=1&id={0}&retorno=licitacoes_do_dia.asp";
        internal static int CRJ_CIDADE_FONTE = 7043;
        internal static string CRJ_DOWNLOAD_ARQUIVO = "http://arquivossiga.proderj.rj.gov.br/siga_imagens//DownlPublic.asp?CripC={0}&CripA={1}&PathFisico=";
        internal static string CRJ_LINK_LICITACAO = "https://www.compras.rj.gov.br/Portal-Siga/EditaisLicitacoes/listar.action";
        internal static string CRJ_FILELINK_REGEX = "(href=\")(.*?)(><i class=\"material-icons\")";

        /*CONSTANTES DIÁRIO OFICIAL DA UNIÃO*/
        internal static string DOU_NOME = "DOU";
        internal static string DOU_HOST = "http://portal.imprensanacional.gov.br/web/guest/inicio";
        internal static string DOU_SECAO_3 = "http://www.imprensanacional.gov.br/leiturajornal?data={0}-{1}-{2}&secao=dou3";
        internal static string DOU_BASE_URL = "http://portal.imprensanacional.gov.br/{0}";
        internal static int DOU_ID_FONTE = 91;
        internal static List<string> DOU_VALIDATION_KEYWORDS = new List<string>
        {
            "AVISO DE CHAMAMENTO PÚBLICO", "PREGÃO", "RDC", "CONCORRÊNCIA", "TOMADA DE PREÇOS", "AVISO DE REABERTURA DE PRAZO",
            "CHAMADA PÚBLICA", "AVISO DE LICITAÇÃO", "LEILÃO", "CHAMAMENTO", "RETIFICAÇÃO", "ADIAMENTO", "PRORROGAÇÃO",
            "LICITAÇÃO Nº", "REABERTURA", "PLANEJAMENTO DE COMPRA", "VENDA DIRETA", "ALIENAÇÃO", "CONSULTA DE PREÇOS",
            "PREGÃO PRESENCIAL", "PREGÃO ELETRÔNICO", "MANIFESTAÇÃO DE INTERESSE", "LICITAÇÃO PÚBLICA NACIONAL",
            "LICITAÇÃO PUBLICA INTERNACIONAL", "DISPENSA DE LICITAÇÃO", "CREDENCIAMENTO", "COTAÇÃO ELETRÔNICA",
            "RITO ORDINÁRIO", "COTAÇÃO DE PREÇO", "CONCURSO", "COMPRA DIRETA", "COMPARAÇÃO DE PREÇOS", "CARTA CONVITE",
            "CONVITE", "SELEÇÃO DE CONSULTORES INDIVIDUAIS", "SELEÇÃO PÚBLICA DE FORNECEDORES", "PRÉ-QUALIFICAÇÃO",
            "DESFAZIMENTO DE BENS", "SHOPPING", "COMUNICADO PÚBLICO N"
        };

        internal static List<string> DOU_BLOCKING_KEYWORDS = new List<string>
        {
            "ADITIVO AO CONTRATO", "AVISO DE ADENDO MODIFICADOR", "AVISO DE ADJUDICAÇÃO E HOMOLOGAÇÃO", "AVISO DE ANULAÇÃO", "AVISO DE HOMOLOGAÇÃO",
            "AVISO DE PENALIDADE", "AVISO DE REGISTRO DE CHAPA", "AVISO DE REVOGAÇÃO", "AVISO DE SORTEIO", "AVISO DE SUSPENSÃO", "EDITAL DE CONVOCAÇÃO",
            "EDITAL DE INTIMAÇÃO", "EDITAL DE NOTIFICAÇÃO", "EXTRATO DE ADENDO MODIFICADOR", "EXTRATO DE ACORDO DE COOPERAÇÃO TÉCNICA", "EXTRATO DE APOSTILAMENTO",
            "EXTRATO DE CESSÃO DE USO", "EXTRATO DE COMPROMISSO", "EXTRATO DE COMPROMISSO DE CONFIDENCIALIDADE", "EXTRATO DE CONCESSÃO", "EXTRATO DE CONTRATO",
            "EXTRATO DE CONVÊNIO", "EXTRATO DE CONVOCAÇÃO", "EXTRATO DE DISPENSA DE LICITAÇÃO", "EXTRATO DE INEXIGIBILIDADE", "EXTRATO DE NOTA DE EMPENHO",
            "EXTRATO DE NOTIFICAÇÃO", "EXTRATO DE INEXIGIBILIDADE DE LICITAÇÃO", "EXTRATO DE PRORROGAÇÃO DE OFÍCIO", "EXTRATO DE PROTOCOLO DE INTENÇÕES",
            "EXTRATO DE REGISTRO DE PREÇOS", "EXTRATO DE RESCISÃO", "EXTRATO DE TERMO ADITIVO", "EXTRATO DE TERMO DE APOSTILAMENTO", "EXTRATO DE TERMO DE COOPERAÇÃO TÉCNICA",
            "EXTRATO DE TERMO DE EXECUÇÃO DESCENTRALIZADA", "EXTRATO DE TERMO DE FOMENTO", "EXTRATO DO 2º TERMO DE APOSTILAMENTO AO CONTRATO", "EXTRATO DO TERMO DE AUTORIZAÇÃO",
            "EXTRATO TERMO DE CESSÃO DE USO", "EXTRATO TERMO DE ADESÃO", "EXTRATOS DE INSTRUMENTOS CONTRATUAIS", "RESULTADO DA HABILITAÇÃO", "RESULTADO DE JULGAMENTO", "REVOGA-SE",
            "REVOGAÇÃO", "RESULTADO FINAL", "SUSPENSÂO", "UASG", "www.comprasnet.gov.br", "www.comprasgovernamentais.gov.br", "www.licitacoes-e.com.br", "https://www.portaldecompraspublicas.com.br",
            "http://www.tcm.ce.gov.br", "http://www.bll.org.br", "convocadas as licitantes habilitadas", "LTDA", "HABILITOU"
        };

        /*CONSTANTES BLL*/
        internal static string BLL_NOME = "BLL";
        internal static string BLL_HOST = "http://bll.org.br/";
        internal static string BLL_ACESSO_PUBLICO = "http://lanceeletronico.cloudapp.net/#/Home";

    }
}

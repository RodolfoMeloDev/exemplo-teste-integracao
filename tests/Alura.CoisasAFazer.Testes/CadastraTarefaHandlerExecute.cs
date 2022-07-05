using Alura.CoisasAFazer.Core.Commands;
using Alura.CoisasAFazer.Core.Models;
using Alura.CoisasAFazer.Infrastructure;
using Alura.CoisasAFazer.Services.Handlers;
using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Alura.CoisasAFazer.Testes
{
    public class CadastraTarefaHandlerExecute
    {
        [Fact]
        public void DadaTarefaComInfoValidasDeveIncluirNoBD()
        {
            //arrange
            var comando = new CadastraTarefa("Estudar Xunit", new Categoria("Estudo"), new DateTime(2019, 12, 31));

            var mock = new Mock<ILogger<CadastraTarefaHandler>>();

            var options = new DbContextOptionsBuilder<DbTarefasContext>()
                .UseInMemoryDatabase("dbTarefasContext")
                .Options;
            var contexto = new DbTarefasContext(options);
            var repo = new RepositorioTarefa(contexto);

            var handler = new CadastraTarefaHandler(repo, mock.Object);

            //act
            handler.Execute(comando); //SUT >> CadastraTarefaHandlerExecute

            //assert
            var tarefa = repo.ObtemTarefas(t => t.Titulo == "Estudar Xunit").FirstOrDefault();
            Assert.NotNull(tarefa);
        }

        delegate void CapturaMensagemLog(LogLevel leve, EventId eventId, object state, Exception exception,
            Func<object, Exception, string> function);

        [Fact]
        public void DadaTarefaComInfoValidasDeveLogar()
        {
            //arrange
            var tituloTarefaEstudado = "Usar Moq para aprofundar conhecimento de APi";
            var comando = new CadastraTarefa(tituloTarefaEstudado, new Categoria("Estudo"), new DateTime(2019, 12, 31));

            var mockLogger = new Mock<ILogger<CadastraTarefaHandler>>();

            LogLevel levelCapturado = LogLevel.Error;
            string mensagemCapturada = string.Empty;

            CapturaMensagemLog captura = (level, eventid, state, exception, func) =>
            {
                levelCapturado = level;
                mensagemCapturada = func(state, exception);
            };

            mockLogger.Setup(l =>
                l.Log(It.IsAny<LogLevel>(),
                      It.IsAny<EventId>(),
                      It.IsAny<object>(),
                      It.IsAny<Exception>(),
                      It.IsAny<Func<object, Exception, string>>()
                     )).Callback(captura);

            var mock = new Mock<IRepositorioTarefas>();
            
            var handler = new CadastraTarefaHandler(mock.Object, mockLogger.Object);

            //act
            handler.Execute(comando); //SUT >> CadastraTarefaHandlerExecute

            //assert
            Assert.Equal(LogLevel.Debug, levelCapturado);
            Assert.Contains(tituloTarefaEstudado, mensagemCapturada);
        }

        [Fact]
        public void QuandoExceptionForLancadaResultadoIsSucessDeveSerFalse()
        {
            //arrange
            var comando = new CadastraTarefa("Estudar Xunit", new Categoria("Estudo"), new DateTime(2019, 12, 31));

            var mock = new Mock<IRepositorioTarefas>();
            var mockLogger = new Mock<ILogger<CadastraTarefaHandler>>();

            mock.Setup(r => r.IncluirTarefas(It.IsAny<Tarefa[]>()))
                .Throws(new Exception("Houve um erro na inclusão de tarefas"));

            var repo = mock.Object;            

            var handler = new CadastraTarefaHandler(repo, mockLogger.Object);

            //act
            CommandResult resultado = handler.Execute(comando);

            //assert
            Assert.False(resultado.IsSuccess);
        }

        [Fact]
        public void QuandoExceptionForLancadaDeveLogarAMensagemDaExcecao()
        {
            //arrange
            var mensagemDeErroEsperada = "Houve um erro na inclusão de tarefas";
            var excessaoEsperada = new Exception(mensagemDeErroEsperada);

            var comando = new CadastraTarefa("Estudar Xunit", new Categoria("Estudo"), new DateTime(2019, 12, 31));

            var mock = new Mock<IRepositorioTarefas>();
            var mockLogger = new Mock<ILogger<CadastraTarefaHandler>>();

            mock.Setup(r => r.IncluirTarefas(It.IsAny<Tarefa[]>()))
                .Throws(excessaoEsperada);

            var repo = mock.Object;

            var handler = new CadastraTarefaHandler(repo, mockLogger.Object);

            //act
            CommandResult resultado = handler.Execute(comando);

            //assert
            mockLogger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<object>(), 
                                        excessaoEsperada, It.IsAny<Func<object, Exception, string>>()), 
                              Times.Once());

        }
    }
}

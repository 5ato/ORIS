using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MyORMLibrary.Tests
{
    [TestClass]
    public class ORMContextTests
    {
        private Mock<IConnectionFactory> _mockConnectionFactory;
        private Mock<IDbConnection> _mockConnection;
        private Mock<IDbCommand> _mockCommand;
        private Mock<IDataParameterCollection> _mockParameters;
        private Mock<IDataReader> _mockReader;
        private ORMContext _ormContext;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockConnectionFactory = new Mock<IConnectionFactory>();
            _mockConnection = new Mock<IDbConnection>();
            _mockCommand = new Mock<IDbCommand>();
            _mockParameters = new Mock<IDataParameterCollection>();
            _mockReader = new Mock<IDataReader>();

            _mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(_mockConnection.Object);
            _mockConnection.Setup(c => c.CreateCommand()).Returns(_mockCommand.Object);
            _mockCommand.SetupGet(c => c.Parameters).Returns(_mockParameters.Object);

            _ormContext = new ORMContext(_mockConnectionFactory.Object);
        }

        [TestMethod]
        public void Create_ShouldExecuteInsertCommand()
        {
            // Arrange
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var tableName = "Users";
            var parameters = new List<IDbDataParameter>();

            _mockConnection.Setup(c => c.Open());
            _mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>());
            _mockCommand.Setup(c => c.CreateParameter()).Returns(() =>
            {
                var param = new Mock<IDbDataParameter>();
                parameters.Add(param.Object);
                return param.Object;
            });
            _mockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>()));
            _mockCommand.Setup(c => c.ExecuteNonQuery());

            // Act
            _ormContext.Create(user, tableName);

            // Assert
            _mockConnection.Verify(c => c.Open(), Times.Once);
            _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains("INSERT INTO Users")), Times.Once);
            _mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once);
            Assert.AreEqual(4, parameters.Count); // Id, FirstName, LastName, Email
        }

        [TestMethod]
        public void ReadById_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var userId = 1;
            var tableName = "Users";
            var readerData = new List<object[]>
            {
                new object[] { "Id", 1 },
                new object[] { "FirstName", "John" },
                new object[] { "LastName", "Doe" },
                new object[] { "Email", "john@example.com" }
            };
            var currentIndex = -1;

            _mockConnection.Setup(c => c.Open());
            _mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>());
            _mockCommand.Setup(c => c.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
            _mockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>()));

            _mockReader.Setup(r => r.Read()).Returns(() =>
            {
                currentIndex++;
                return currentIndex < 1; // Only one row
            });
            _mockReader.Setup(r => r.FieldCount).Returns(4);
            _mockReader.Setup(r => r.GetName(It.IsAny<int>())).Returns<int>(i => (string)readerData[i][0]);
            _mockReader.Setup(r => r.GetValue(It.IsAny<int>())).Returns<int>(i => readerData[i][1]);
            _mockCommand.Setup(c => c.ExecuteReader()).Returns(_mockReader.Object);

            // Act
            var result = _ormContext.ReadById<User>(userId, tableName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Id);
            Assert.AreEqual("John", result.FirstName);
            Assert.AreEqual("Doe", result.LastName);
            Assert.AreEqual("john@example.com", result.Email);
        }

        [TestMethod]
        public void ReadById_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = 999;
            var tableName = "Users";

            _mockConnection.Setup(c => c.Open());
            _mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>());
            _mockCommand.Setup(c => c.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
            _mockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>()));
            _mockReader.Setup(r => r.Read()).Returns(false);
            _mockCommand.Setup(c => c.ExecuteReader()).Returns(_mockReader.Object);

            // Act
            var result = _ormContext.ReadById<User>(userId, tableName);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ReadAll_ShouldReturnListOfUsers()
        {
            // Arrange
            var tableName = "Users";
            var readerData = new[]
            {
                new object[] { 1, "John", "Doe", "john@example.com" },
                new object[] { 2, "Jane", "Smith", "jane@example.com" }
            };
            var currentRow = -1;

            _mockConnection.Setup(c => c.Open());
            _mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>());

            _mockReader.Setup(r => r.Read()).Returns(() =>
            {
                currentRow++;
                return currentRow < readerData.Length;
            });
            _mockReader.Setup(r => r.FieldCount).Returns(4);
            _mockReader.Setup(r => r.GetName(It.IsAny<int>())).Returns<int>(i =>
                i == 0 ? "Id" : i == 1 ? "FirstName" : i == 2 ? "LastName" : "Email");
            _mockReader.Setup(r => r.GetValue(It.IsAny<int>())).Returns<int>(i => readerData[currentRow][i]);
            _mockCommand.Setup(c => c.ExecuteReader()).Returns(_mockReader.Object);

            // Act
            var result = _ormContext.ReadByAll<User>(tableName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            var firstUser = result[0];
            Assert.AreEqual(1, firstUser.Id);
            Assert.AreEqual("John", firstUser.FirstName);
            Assert.AreEqual("Doe", firstUser.LastName);
            Assert.AreEqual("john@example.com", firstUser.Email);

            var secondUser = result[1];
            Assert.AreEqual(2, secondUser.Id);
            Assert.AreEqual("Jane", secondUser.FirstName);
            Assert.AreEqual("Smith", secondUser.LastName);
            Assert.AreEqual("jane@example.com", secondUser.Email);
        }

        [TestMethod]
        public void Update_ShouldExecuteUpdateCommand()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var tableName = "Users";
            var parameters = new List<IDbDataParameter>();

            _mockConnection.Setup(c => c.Open());
            _mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>());
            _mockCommand.Setup(c => c.CreateParameter()).Returns(() =>
            {
                var param = new Mock<IDbDataParameter>();
                parameters.Add(param.Object);
                return param.Object;
            });
            _mockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>()));
            _mockCommand.Setup(c => c.ExecuteNonQuery());

            // Act
            _ormContext.Update(userId, user, tableName);

            // Assert
            _mockConnection.Verify(c => c.Open(), Times.Once);
            _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains("UPDATE Users") && s.Contains("WHERE Id = @id")), Times.Once);
            _mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once);
            Assert.AreEqual(5, parameters.Count); // id parameter + 4 properties
        }

        [TestMethod]
        public void Delete_ShouldExecuteDeleteCommand()
        {
            // Arrange
            var userId = 1;
            var tableName = "Users";

            _mockConnection.Setup(c => c.Open());
            _mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>());
            _mockCommand.Setup(c => c.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
            _mockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>()));
            _mockCommand.Setup(c => c.ExecuteNonQuery());

            // Act
            _ormContext.Delete(userId, tableName);

            // Assert
            _mockConnection.Verify(c => c.Open(), Times.Once);
            _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains("DELETE FROM Users") && s.Contains("WHERE Id = @id")), Times.Once);
            _mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once);
        }

        [TestMethod]
        public void Create_WithUsersWithoutId_ShouldExecuteInsertCommandWithoutId()
        {
            // Arrange
            var user = new UsersWithoudId { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var tableName = "Users";
            var parameters = new List<IDbDataParameter>();

            _mockConnection.Setup(c => c.Open());
            _mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>());
            _mockCommand.Setup(c => c.CreateParameter()).Returns(() =>
            {
                var param = new Mock<IDbDataParameter>();
                parameters.Add(param.Object);
                return param.Object;
            });
            _mockParameters.Setup(p => p.Add(It.IsAny<IDbDataParameter>()));
            _mockCommand.Setup(c => c.ExecuteNonQuery());

            // Act
            _ormContext.Create(user, tableName);

            // Assert
            _mockConnection.Verify(c => c.Open(), Times.Once);
            _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains("INSERT INTO Users")), Times.Once);
            _mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once);
            Assert.AreEqual(3, parameters.Count); // Only FirstName, LastName, Email (no Id)
        }

        [TestMethod]
        public void FirstOrDefault_UserEmail_ReturnUserModelOrNull()
        {
            TestFirstOrDefault(u => u.Email == "Aboba@aboba.aba");
        }

        [TestMethod]
        public void FirstOrDefault_UserId_ReturnUserModelOrNull()
        {
            TestFirstOrDefault(u => u.Id == 1);
        }

        public void TestFirstOrDefault(Expression<Func<User, bool>> predicate)
        {
            var readerData = new List<object[]>
            {
                new object[] { "Id", 1 },
                new object[] { "Email", "Aboba@aboba.aba" },
                new object[] { "FirstName", "aboba" },
                new object[] { "LastName", "biba" },
            };

            _mockConnection.Setup(c => c.Open());

            _mockCommand.SetupSet(c => c.CommandText = It.IsAny<string>());
            _mockCommand.Setup(c => c.ExecuteReader()).Returns(_mockReader.Object);

            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.SetupGet(r => r.FieldCount).Returns(4);
            _mockReader.Setup(r => r.GetName(It.IsAny<int>())).Returns<int>(i => (string)readerData[i][0]);
            _mockReader.Setup(r => r.GetValue(It.IsAny<int>())).Returns<int>(i => readerData[i][1]);

            // Arrange
            var userAssert = new User() { Id = 1, Email = "Aboba@aboba.aba", FirstName = "aboba", LastName = "biba" };
            var context = new ORMContext(_mockConnectionFactory.Object);

            // Act
            var result = context.FirstOrDefault<User>(predicate);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userAssert.Id, result.Id);
            Assert.AreEqual(userAssert.FirstName, result.FirstName);
            Assert.AreEqual(userAssert.Email, result.Email);
            Assert.AreEqual(userAssert.LastName, result.LastName);
        }
    }
}
﻿using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Extensions.DependencyInjection.ChildContainers.Tests;

internal class IServiceCollectionExtensionsTests
{
    private IServiceCollection _serviceCollection;

    [SetUp]
    public void SetUp()
    {
        _serviceCollection = new ServiceCollection();
    }

    [Test]
    public void AddChildContainer_InBuilder_ResolvesSingletonService()
    {
        //Arrange
        _serviceCollection
            .AddSingleton<TestA>(e => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                var testA = parentServices.GetRequiredService<TestA>();
                childServices.AddSingleton<TestB>(e => new TestB() { Data = testA.Data });
            })
            .ForwardSingleton<TestB>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredService<TestA>();
        var testB = provider.GetRequiredService<TestB>();

        //Assert
        Assert.That(testB, Is.Not.Null);
        Assert.That(testB.Data, Is.EqualTo(testA.Data));
    }

    [Test]
    public void AddChildContainer_InBuilder_ResolvesTransientService()
    {
        //Arrange
        _serviceCollection
            .AddTransient<TestA>(e => new TestA() { Data = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                var testA = parentServices.GetRequiredService<TestA>();
                childServices.AddTransient<TestB>(e => new TestB() { Data = testA.Data });
            })
            .ForwardTransient<TestB>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA = provider.GetRequiredService<TestA>();
        var testB = provider.GetRequiredService<TestB>();

        //Assert
        Assert.That(testB, Is.Not.Null);
        Assert.That(testB.Data, Is.EqualTo(testA.Data));
    }

    [Test]
    public void AddChildContainer_InImport_ResolvesSingletonServices()
    {
        //Arrange
        _serviceCollection
            .AddSingleton<Child>(e => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddSingleton<Parent>();
            })
            .ImportSingleton<Child>()
            .ForwardSingleton<Parent>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var parent = provider.GetRequiredService<Parent>();
        var child = provider.GetRequiredService<Child>();

        //Assert
        Assert.That(parent, Is.Not.Null);
        Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
    }

    [Test]
    public void AddChildContainer_InImport_ResolvesScopedServices()
    {
        //Arrange
        _serviceCollection
            .AddScoped<Child>(e => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<Parent>();
            })
            .ImportScoped<Child>()
            .ForwardScoped<Parent>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider()
            .CreateScope().ServiceProvider;

        var parent = provider.GetRequiredService<Parent>();
        var child = provider.GetRequiredService<Child>();

        //Assert
        Assert.That(parent, Is.Not.Null);
        Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
    }

    [Test]
    public void AddChildContainer_InImport_ResolvesTransientServices()
    {
        //Arrange
        _serviceCollection
            .AddTransient<Child>(e => new Child() { Name = "Test" })
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddTransient<Parent>();
            })
            .ImportTransient<Child>()
            .ForwardTransient<Parent>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var parent = provider.GetRequiredService<Parent>();
        var child = provider.GetRequiredService<Child>();

        //Assert
        Assert.That(parent, Is.Not.Null);
        Assert.That(parent.Child.Name, Is.EqualTo(child.Name));
    }

    [Test]
    public void AddChildContainer_NoForward_DoesNotResolveServices()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddTransient<TestA>(e => new TestA() { Data = "Test" });
                childServices.AddTransient<TestB>(e => new TestB() { Data = "Test" });
            })
            .ForwardTransient<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        //Assert
        Assert.DoesNotThrow(() => provider.GetRequiredService<TestA>());
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<TestB>());
    }

    [Test]
    public void AddChildContainer_ForwardSingleton_IsSingleton()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddSingleton<TestA>(e => new TestA() { Data = "Test" });
            })
            .ForwardSingleton<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA_1 = provider.GetRequiredService<TestA>();
        var testA_2 = provider.GetRequiredService<TestA>();

        //Assert
        Assert.That(testA_1, Is.Not.Null);
        Assert.That(testA_1, Is.EqualTo(testA_2));
    }

    [Test]
    public void AddChildContainer_ForwardScoped_IsScoped()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddScoped<TestA>(e => new TestA() { Data = "Test" });
            })
            .ForwardScoped<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var scope_1 = provider.CreateScope().ServiceProvider;

        var testA_1_1 = scope_1.GetRequiredService<TestA>();
        var testA_1_2 = scope_1.GetRequiredService<TestA>();

        var scope_2 = provider.CreateScope().ServiceProvider;

        var testA_2_1 = scope_2.GetRequiredService<TestA>();
        var testA_2_2 = scope_2.GetRequiredService<TestA>();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(testA_1_1, Is.Not.Null);
            Assert.That(testA_1_1, Is.EqualTo(testA_1_2));

            Assert.That(testA_2_1, Is.Not.Null);
            Assert.That(testA_2_1, Is.EqualTo(testA_2_2));

            Assert.That(testA_1_1, Is.Not.EqualTo(testA_2_1));
        });
    }

    [Test]
    public void AddChildContainer_ForwardTransient_IsTransient()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddTransient<TestA>(e => new TestA() { Data = "Test" });
            })
            .ForwardTransient<TestA>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        var testA_1 = provider.GetRequiredService<TestA>();
        var testA_2 = provider.GetRequiredService<TestA>();

        //Assert
        Assert.That(testA_1, Is.Not.Null);
        Assert.That(testA_1, Is.Not.EqualTo(testA_2));
    }

    [Test]
    public void AddChildContainer_RecursiveServices_ThrowsCircularDependencyException()
    {
        //Arrange
        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
            {
                childServices.AddSingleton<Parent>();
            })
            .ImportSingleton<Child>()
            .ForwardSingleton<Parent>();

        _serviceCollection
            .AddChildContainer((childServices, parentServices) =>
             {
                 childServices.AddSingleton<Child, RecursiveChild>();
             })
            .ImportSingleton<Parent>()
            .ForwardSingleton<Child>();

        //Act
        var provider = _serviceCollection.BuildServiceProvider();

        //Assert
        Assert.Multiple(() =>
        {
            Assert.Throws<CircularDependencyException>(() =>
            {
                provider.GetRequiredService<Parent>();
            });

            Assert.Throws<CircularDependencyException>(() =>
            {
                provider.GetRequiredService<Child>();
            });
        });
    }

    private class TestA
    {
        public required string Data { get; set; }
    }

    private class TestB
    {
        public required string Data { get; set; }
    }

    private class Parent
    {
        public Child Child { get; }

        public Parent(Child child)
        {
            Child = child;
        }
    }

    private class Child
    {
        public required string Name { get; set; }
    }

    private class RecursiveChild : Child
    {
        public Parent Parent { get; }

        public RecursiveChild(Parent parent)
        {
            Parent = parent;
        }
    }
}

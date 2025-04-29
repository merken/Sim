# Sim, an API Simulator 

Build the sim docker image from the ./src directory:

```
docker build -f Sim.Api/Dockerfile -t sim .
```

Run 3 containers for the CartApi, ProductsApi and PaymentApi:
```
docker run -d -p 5010:8080 --name cartapi sim
```

```
docker run -d -p 5011:8080 --name productsapi sim
```

```
docker run -d -p 5012:8080 --name payment sim
```

After that you can run the MyShop project:
```
dotnet run --project ./src/MyShop
```

Setup all the calls in the shop.http file.

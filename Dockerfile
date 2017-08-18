FROM microsoft/dotnet
 
COPY AppleWatchAPI/ /dotnetapp
WORKDIR /dotnetapp

RUN dotnet restore

ENV ASPNETCORE_URLS http://+:5000 
EXPOSE 5000/tcp

ENTRYPOINT dotnet run
FROM node:18

RUN apt-get update && apt-get install -y dos2unix bash curl && rm -rf /var/lib/apt/lists/*

# Create app directory
WORKDIR /usr/src/app

# Install app dependencies
COPY package*.json ./

RUN npm install

# Copy app source
COPY . .

# convert line endings to unix
RUN dos2unix ./entrypoint.sh 
RUN chmod +x ./entrypoint.sh

EXPOSE 3000

ENTRYPOINT ["./entrypoint.sh", "azurecosmosemulator", "8081"]

FROM node:22-alpine AS dev
WORKDIR /app

# Copy package files for layer caching
COPY package.json package-lock.json* ./
RUN npm install

# Copy source (bind mount in compose.yaml will override in dev)
COPY . .

EXPOSE 5174
CMD ["npm", "run", "dev"]

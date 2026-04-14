FROM node:22-alpine AS dev
WORKDIR /app

# Copy package files for layer caching
COPY package.json package-lock.json* ./
RUN npm install

# Copy source (bind mount in compose.yaml will override in dev)
COPY . .

# Fix permissions so node user can write to /app (needed for Vinxi dev mode)
RUN chown -R node:node /app

EXPOSE 5173
USER node
CMD ["npm", "run", "dev"]

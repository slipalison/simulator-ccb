FROM node:22-alpine AS dev
WORKDIR /app

# Copy package files for layer caching
COPY --chown=node:node package.json package-lock.json* ./
RUN npm install

# Copy source with correct ownership (bind mount in compose.yaml will override in dev)
COPY --chown=node:node . .

EXPOSE 5174
USER node
CMD ["npm", "run", "dev"]

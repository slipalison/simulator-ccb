FROM node:22-alpine AS dev

# Create workdir owned by node before switching users
RUN mkdir -p /app && chown node:node /app
WORKDIR /app
USER node

# Copy package files for layer caching
COPY --chown=node:node package.json package-lock.json ./
RUN npm ci

# Copy source with correct ownership (bind mount in compose.yaml will override in dev)
COPY --chown=node:node . .

EXPOSE 5173
CMD ["npm", "run", "dev"]

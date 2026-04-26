FROM node:22-alpine AS dev

# Create workdir owned by node before switching users
RUN mkdir -p /app && chown node:node /app
WORKDIR /app
USER node

# Copy package files for layer caching
COPY --chown=node:node package.json ./
COPY --chown=node:node package-lock.json* ./
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

# Copy source with correct ownership (bind mount in compose.yaml will override in dev)
COPY --chown=node:node . .

EXPOSE 5174
CMD ["npm", "run", "dev"]

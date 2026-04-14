FROM node:22-alpine AS dev
WORKDIR /app

# Copy package files for layer caching
COPY package.json package-lock.json* ./
RUN npm install

# Copy source (bind mount in compose.yaml will override in dev)
COPY . .

# Fix permissions only on source dirs (skip node_modules — already owned correctly)
# This avoids the slow chown -R on the entire /app directory
RUN find /app -maxdepth 1 -not -name node_modules -not -name . -exec chown -R node:node {} + 2>/dev/null || true
RUN chown -R node:node /app/src /app/public /app/*.ts /app/*.json /app/*.mjs /app/*.css 2>/dev/null || true

EXPOSE 5174
USER node
CMD ["npm", "run", "dev"]

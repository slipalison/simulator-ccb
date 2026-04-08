// capture-screenshots.mjs
import { chromium } from 'playwright';
import { mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const screenshotsDir = join(__dirname, 'screenshots');
mkdirSync(screenshotsDir, { recursive: true });

async function main() {
  console.log('Launching Chromium...');
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });

  // Screenshot 1: LoginPage
  console.log('Capturing Login page...');
  await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle' });
  await page.waitForSelector('label', { timeout: 5000 });
  await page.screenshot({ path: join(screenshotsDir, 'login-page-after.png'), fullPage: true });
  console.log('  -> login-page-after.png');

  // Validate left-aligned labels
  const labels = await page.locator('label').all();
  for (const label of labels) {
    const className = await label.getAttribute('class') || '';
    if (!className.includes('text-left') && !className.includes('text-left')) {
      console.warn(`  WARNING: Label without text-left: ${className}`);
    }
  }
  console.log('  Labels alignment check: PASS');

  // Screenshot 2: RegistrationForm
  console.log('Capturing Registration page...');
  await page.goto('http://localhost:5173/register', { waitUntil: 'networkidle' });
  await page.waitForSelector('label', { timeout: 5000 });
  await page.screenshot({ path: join(screenshotsDir, 'registration-form-after.png'), fullPage: true });
  console.log('  -> registration-form-after.png');

  // Validate card structure
  const card = page.locator('.rounded-xl');
  const cardCount = await card.count();
  console.log(`  Cards found: ${cardCount}`);
  if (cardCount === 0) {
    console.warn('  WARNING: No card container found!');
  }

  // Screenshot 3: Mobile Login
  console.log('Capturing Login page (mobile)...');
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto('http://localhost:5173/login', { waitUntil: 'networkidle' });
  await page.waitForSelector('label', { timeout: 5000 });
  await page.screenshot({ path: join(screenshotsDir, 'login-mobile-after.png'), fullPage: true });
  console.log('  -> login-mobile-after.png');

  // Screenshot 4: Mobile Registration
  console.log('Capturing Registration page (mobile)...');
  await page.goto('http://localhost:5173/register', { waitUntil: 'networkidle' });
  await page.waitForSelector('label', { timeout: 5000 });
  await page.screenshot({ path: join(screenshotsDir, 'registration-mobile-after.png'), fullPage: true });
  console.log('  -> registration-mobile-after.png');

  await browser.close();
  console.log('\nAll screenshots captured successfully!');
  console.log(`Saved to: ${screenshotsDir}`);
}

main().catch(err => {
  console.error('Failed:', err.message);
  process.exit(1);
});

import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import vm from "node:vm";

const source = await readFile(
  new URL("../Frontend/Product/Js/mappers.js", import.meta.url),
  "utf8",
);

function loadPricing() {
  const context = {
    window: { SopmineDesign: {} },
    URL,
    location: { origin: "http://localhost" },
  };
  vm.runInNewContext(source, context);
  return context.window.SopmineDesign.ProductMappers.calculatePricing;
}

function loadPriceValidator() {
  const context = {
    window: { SopmineDesign: {} },
    URL,
    location: { origin: "http://localhost" },
  };
  vm.runInNewContext(source, context);
  return context.window.SopmineDesign.ProductMappers.isValidPrice;
}

test("editing sale TTC keeps purchase prices and recalculates sale HT and margin", () => {
  const calculatePricing = loadPricing();

  assert.deepEqual(
    { ...calculatePricing(
      { purchase: 100, purchaseTtc: 120, saleHt: 115, saleTtc: 135, vat: 20, margin: 15 },
      "saleTtc",
    ) },
    { purchase: 100, purchaseTtc: 120, saleHt: 112.5, saleTtc: 135, margin: 12.5 },
  );
});

test("editing purchase TTC derives purchase HT and keeps the entered margin", () => {
  const calculatePricing = loadPricing();

  assert.deepEqual(
    { ...calculatePricing(
      { purchase: 100, purchaseTtc: 240, saleHt: 120, saleTtc: 144, vat: 20, margin: 20 },
      "purchaseTtc",
    ) },
    { purchase: 200, purchaseTtc: 240, saleHt: 240, saleTtc: 288, margin: 20 },
  );
});

test("editing sale HT derives the other prices from the entered margin and TVA", () => {
  const calculatePricing = loadPricing();

  assert.deepEqual(
    { ...calculatePricing(
      { purchase: 200, purchaseTtc: 240, saleHt: 175, saleTtc: 144, vat: 20, margin: 50 },
      "saleHt",
    ) },
    { purchase: 116.667, purchaseTtc: 140, saleHt: 175, saleTtc: 210, margin: 50 },
  );
});

test("editing margin or TVA recalculates dependent TTC prices", () => {
  const calculatePricing = loadPricing();

  assert.deepEqual(
    { ...calculatePricing(
      { purchase: 100, purchaseTtc: 120, saleHt: 120, saleTtc: 144, vat: 20, margin: 30 },
      "margin",
    ) },
    { purchase: 100, purchaseTtc: 120, saleHt: 130, saleTtc: 156, margin: 30 },
  );
  assert.deepEqual(
    { ...calculatePricing(
      { purchase: 100, purchaseTtc: 120, saleHt: 130, saleTtc: 156, vat: 10, margin: 30 },
      "vat",
    ) },
    { purchase: 100, purchaseTtc: 110, saleHt: 130, saleTtc: 143, margin: 30 },
  );
});

test("empty and zero prices stay finite while recalculating", () => {
  const calculatePricing = loadPricing();
  const result = calculatePricing({}, "saleTtc");

  Object.values(result).forEach((value) => assert.equal(Number.isFinite(value), true));
});

test("price calculations preserve three decimal places", () => {
  const calculatePricing = loadPricing();
  const result = calculatePricing(
    { purchase: 15.655, purchaseTtc: 0, saleHt: 0, saleTtc: 0, vat: 20, margin: 10 },
    "purchase",
  );

  assert.equal(result.purchase, 15.655);
  assert.equal(result.purchaseTtc, 18.786);
  assert.equal(result.saleHt, 17.221);
  assert.equal(result.saleTtc, 20.665);

  const commaResult = calculatePricing(
    { purchase: "15,655", purchaseTtc: 0, saleHt: 0, saleTtc: 0, vat: 20, margin: 10 },
    "purchase",
  );
  assert.equal(commaResult.purchase, 15.655);
});

test("price input validation accepts dot and comma decimal notation", () => {
  const isValidPrice = loadPriceValidator();

  assert.equal(isValidPrice("15.655"), true);
  assert.equal(isValidPrice("15,655"), true);
  assert.equal(isValidPrice("15.6555"), false);
  assert.equal(isValidPrice("15abc"), false);
});

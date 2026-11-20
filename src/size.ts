export const widgetWidth = 200;
export const widgetHeight = 250;
export const isCompact = (width: number) => width <= 160;

export function dimensions(requestedWidth: number) {
  const width = Math.max(120, Math.min(360, requestedWidth));
  return { width, height: width * widgetHeight / widgetWidth };
}

export function widthFromDrag(width: number, dx: number, dy: number) {
  const distance = (dx * widgetWidth + dy * widgetHeight) / (widgetWidth ** 2 + widgetHeight ** 2);
  return dimensions(width + distance * widgetWidth).width;
}

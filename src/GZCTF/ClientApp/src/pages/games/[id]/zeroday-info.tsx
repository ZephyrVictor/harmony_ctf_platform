import { Alert, Card, Center, Group, Loader, Stack, Title } from '@mantine/core'
import { mdiAlertCircleOutline, mdiBugCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { Markdown } from '@Components/MarkdownRenderer'
import { WithGameTab } from '@Components/WithGameTab'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useGame } from '@Hooks/useGame'
import api, { Role } from '@Api'

const ZeroDayInfo: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const numId = Number(id)
  const { game } = useGame(numId)

  // 获取队伍信息 + 0day 文案 —— 通过 game detail API（含 zeroDayInfoMarkdown + teamToken）
  const { data: detail } = api.game.useGameChallengesWithTeamInfo(numId, {
    revalidateOnFocus: false,
  })
  const ndayScore = detail?.ndayScore ?? 0
  const zeroDayGate = detail?.zeroDayNdayScoreRequirement ?? 200
  const canSubmitZeroDay = detail?.canSubmitZeroDay ?? (zeroDayGate <= 0 || ndayScore >= zeroDayGate)

  const renderedMarkdown = useMemo(() => {
    const md = detail?.zeroDayInfoMarkdown ?? ''
    if (!md) return ''
    const teamToken = detail?.teamToken ?? ''
    const teamName = detail?.rank?.name ?? ''
    return md
      .replaceAll('{{teamToken}}', teamToken)
      .replaceAll('{{teamName}}', teamName)
  }, [detail?.zeroDayInfoMarkdown, detail?.teamToken, detail?.rank?.name])

  if (!game) {
    return (
      <WithNavBar width="90%" minWidth={0}>
        <Center h="50vh">
          <Loader />
        </Center>
      </WithNavBar>
    )
  }

  return (
    <WithNavBar width="90%" minWidth={0}>
      <WithRole requiredRole={Role.User}>
        <WithGameTab>
          <Stack gap="md" pb="2rem">
            <Card withBorder shadow="sm" p="lg">
              <Group justify="space-between" mb="md">
                <Group gap="sm">
                  <Icon path={mdiBugCheck} size={1.2} />
                  <Title order={3}>{t('game.zeroday_info.title')}</Title>
                </Group>
              </Group>

              {!canSubmitZeroDay ? (
                <Alert color="yellow" icon={<Icon path={mdiAlertCircleOutline} size={1} />}>
                  {t('game.content.zeroday.gate', {
                    current: ndayScore,
                    required: zeroDayGate,
                    defaultValue: `0day submission requires ${zeroDayGate} Nday points. Current Nday score: ${ndayScore}.`,
                  })}
                </Alert>
              ) : renderedMarkdown ? (
                <Markdown source={renderedMarkdown} />
              ) : (
                <Alert color="yellow" icon={<Icon path={mdiAlertCircleOutline} size={1} />}>
                  {t('game.zeroday_info.empty')}
                </Alert>
              )}
            </Card>
          </Stack>
        </WithGameTab>
      </WithRole>
    </WithNavBar>
  )
}

export default ZeroDayInfo
